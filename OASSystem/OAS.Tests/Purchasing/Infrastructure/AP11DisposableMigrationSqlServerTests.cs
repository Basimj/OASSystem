using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Purchasing.Services;

namespace OAS.Tests.Purchasing.Infrastructure;

/// <summary>
/// Opt-in destructive migration rehearsal ONLY against a uniquely named disposable database.
/// The configured connection string supplies the server, NEVER the database to delete.
/// </summary>
[TestFixture, Category("AP11DisposableSqlServer"), NonParallelizable]
public sealed class AP11DisposableMigrationSqlServerTests
{
    private string? _ownedConnection;

    [OneTimeSetUp]
    public async Task CreateDisposableDatabase()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_PURCHASING_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
            Assert.Ignore("Set OAS_PURCHASING_TEST_SQLSERVER to opt in to disposable AP-11 SQL migration rehearsal.");

        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_AP11_{Guid.NewGuid():N}",
            ApplicationName = "OAS-AP11-DisposableMigration"
        };
        _ownedConnection = builder.ConnectionString;
        await using var db = Db();
        // Leave PendingModelChanges checks ENABLED: suppressing that warning hides
        // a genuine deployment risk. If this fails, fix migrations/snapshot first.
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task DeleteOnlyOwnedDatabase()
    {
        if (_ownedConnection is null) return;
        var name = new SqlConnectionStringBuilder(_ownedConnection).InitialCatalog;
        if (!name.StartsWith("OAS_AP11_", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to delete a non-AP11 disposable database.");
        await using var db = Db();
        await db.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task Ap11_AllMigrationsApply_AndCanRunAgainWithoutSchemaDrift()
    {
        await using var db = Db();
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
        Assert.That(applied, Is.Not.Empty);
        Assert.That(pending, Is.Empty, "Migration chain must reach current main before accepting AP-11.");
        await db.Database.MigrateAsync(); // must be safe to retry
        Assert.That((await db.Database.GetPendingMigrationsAsync()), Is.Empty);
        Assert.That((await db.Database.GetAppliedMigrationsAsync()).Count(), Is.EqualTo(applied.Length));
    }

    [Test]
    public async Task Ap11_PurchasingIntegrationJobs_AndAllocationConstraints_AreInstalled()
    {
        await using var db = Db();
        await using var conn = new SqlConnection(_ownedConnection);
        await conn.OpenAsync();
        foreach (var name in new[] { "tbl_PurchasingIntegrationJobs", "tbl_SupplierCreditApplications", "tbl_PaymentAllocations" })
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CASE WHEN OBJECT_ID(N'dbo.' + @name, N'U') IS NULL THEN 0 ELSE 1 END;";
            cmd.Parameters.AddWithValue("@name", name);
            Assert.That(Convert.ToInt32(await cmd.ExecuteScalarAsync()), Is.EqualTo(1), name);
        }
        await using var fk = conn.CreateCommand();
        fk.CommandText = """
SELECT COUNT_BIG(*) FROM sys.foreign_keys WHERE name=N'FK_PurchasingIntegrationJobs_Receipts'
 AND referenced_object_id=OBJECT_ID(N'dbo.tbl_PurchaseReceipts',N'U') AND is_disabled=0;
""";
        Assert.That(Convert.ToInt64(await fk.ExecuteScalarAsync()), Is.EqualTo(1));
        await using var checkSql = conn.CreateCommand();
        checkSql.CommandText = """
SELECT COUNT_BIG(*) FROM sys.check_constraints
WHERE name=N'CK_PaymentAllocations_TargetDocumentAmount_Positive' AND is_disabled=0;
""";
        Assert.That(Convert.ToInt64(await checkSql.ExecuteScalarAsync()), Is.EqualTo(1));
    }

    [Test]
    public async Task Ap11_TwoConcurrentAllocationRequests_CannotHoldTheSameSqlKeyRangeLock()
    {
        await using var first = Db();
        await using var second = Db();
        await first.Database.BeginTransactionAsync();
        await second.Database.BeginTransactionAsync();
        try
        {
            var voucherLine = Guid.NewGuid();
            var invoice = Guid.NewGuid();
            await new PurchasePaymentAllocationConcurrencyPort(first)
                .LockPaymentLineAndInvoiceAsync(voucherLine, invoice);
            await second.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 750;");
            var exception = Assert.ThrowsAsync<SqlException>(async () =>
                await new PurchasePaymentAllocationConcurrencyPort(second)
                    .LockPaymentLineAndInvoiceAsync(voucherLine, invoice));
            Assert.That(exception?.Number, Is.EqualTo(1222),
                "Second allocation must not bypass the lock held by first transaction.");
        }
        finally
        {
            await first.Database.RollbackTransactionAsync();
            await second.Database.RollbackTransactionAsync();
        }
    }

    private OasDbContext Db() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer(_ownedConnection!).Options);
}
