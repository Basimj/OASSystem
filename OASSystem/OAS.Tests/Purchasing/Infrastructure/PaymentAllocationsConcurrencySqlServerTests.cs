using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Purchasing.Services;

namespace OAS.Tests.Purchasing.Infrastructure;

// Opt in with OAS_PURCHASING_TEST_SQLSERVER. The fixture owns and drops its database.
[TestFixture, Category("PurchasingSqlServer"), NonParallelizable]
public sealed class PaymentAllocationsConcurrencySqlServerTests
{
    private const string Ap03MigrationId = "20261017163000_AddPaymentAllocationConcurrencyAndReversal";
    private string _connection = null!;

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_PURCHASING_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_PURCHASING_TEST_SQLSERVER to run Purchasing AP SQL Server tests.");
            return;
        }

        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_AP03_{Guid.NewGuid():N}"
        };
        _connection = builder.ConnectionString;

        await using var context = Context();
        await context.Database.MigrateAsync(Ap03MigrationId);
    }

    [OneTimeTearDown]
    public async Task DropOwnedDatabase()
    {
        if (string.IsNullOrWhiteSpace(_connection)) return;
        await using var context = Context();
        await context.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task Ap03Migration_CreatesAllocationConcurrencyAndReversalSchema()
    {
        await using var connection = new SqlConnection(_connection);
        await connection.OpenAsync();

        Assert.Multiple(async () =>
        {
            Assert.That(await ColumnExists(connection, "tbl_PaymentAllocations", "IdempotencyKey"), Is.True);
            Assert.That(await ColumnExists(connection, "tbl_PaymentAllocations", "ReversedAtUtc"), Is.True);
            Assert.That(await ColumnExists(connection, "tbl_PaymentAllocations", "ReversedBy"), Is.True);
            Assert.That(await ColumnExists(connection, "tbl_PaymentAllocations", "ReversalReason"), Is.True);
            Assert.That(await ColumnExists(connection, "tbl_PaymentAllocations", "RowVersion"), Is.True);
            Assert.That(await IndexExists(connection, "tbl_PaymentAllocations", "UX_PaymentAllocations_IdempotencyKey"), Is.True);
            Assert.That(await IndexExists(connection, "tbl_PaymentAllocations", "IX_PaymentAllocations_PaymentVoucherLine_Date"), Is.True);
            Assert.That(await MigrationApplied(connection, Ap03MigrationId), Is.True);
        });
    }

    [Test]
    public async Task AllocationLock_SamePaymentLineAndInvoice_BlocksConcurrentDecisionUntilFirstTransactionEnds()
    {
        var paymentLineId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        await using var first = Context();
        await using var second = Context();
        await first.Database.BeginTransactionAsync();
        await second.Database.BeginTransactionAsync();

        var firstPort = new PurchasePaymentAllocationConcurrencyPort(first);
        var secondPort = new PurchasePaymentAllocationConcurrencyPort(second);

        await firstPort.LockPaymentLineAndInvoiceAsync(paymentLineId, invoiceId);

        // Keep the acceptance test deterministic: instead of waiting indefinitely for
        // SQL Server, fail the second statement after 500 ms if the first transaction
        // really owns the serializable key-range locks.
        await second.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 500;");
        var exception = Assert.ThrowsAsync<SqlException>(async () =>
            await secondPort.LockPaymentLineAndInvoiceAsync(paymentLineId, invoiceId));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Number, Is.EqualTo(1222), "Expected SQL Server lock-request timeout (1222).");

        await first.Database.RollbackTransactionAsync();
        await second.Database.RollbackTransactionAsync();
    }

    private OasDbContext Context() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer(_connection)
        .Options);

    private static async Task<bool> ColumnExists(SqlConnection connection, string table, string column)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT CASE WHEN EXISTS
(
    SELECT 1 FROM sys.columns c
    JOIN sys.tables t ON t.object_id = c.object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo' AND t.name = @table AND c.name = @column
) THEN 1 ELSE 0 END;
""";
        command.Parameters.AddWithValue("@table", table);
        command.Parameters.AddWithValue("@column", column);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task<bool> IndexExists(SqlConnection connection, string table, string index)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT CASE WHEN EXISTS
(
    SELECT 1 FROM sys.indexes i
    JOIN sys.tables t ON t.object_id = i.object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo' AND t.name = @table AND i.name = @index
) THEN 1 ELSE 0 END;
""";
        command.Parameters.AddWithValue("@table", table);
        command.Parameters.AddWithValue("@index", index);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task<bool> MigrationApplied(SqlConnection connection, string migrationId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = @id) THEN 1 ELSE 0 END;";
        command.Parameters.AddWithValue("@id", migrationId);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }
}
