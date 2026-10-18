using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Purchasing.Infrastructure;

// Runs against an isolated, disposable database. Opt in with OAS_PURCHASING_TEST_SQLSERVER.
[TestFixture, Category("PurchasingSqlServer"), NonParallelizable]
public sealed class SupplierOpeningAp05SqlServerTests
{
    private const string MigrationId = "20261017180000_AddSupplierAgingAndOpeningPayables";
    private string? _connectionString;

    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_PURCHASING_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_PURCHASING_TEST_SQLSERVER to enable isolated AP-05 SQL Server tests.");
            return;
        }

        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_AP05_{Guid.NewGuid():N}"
        };
        _connectionString = builder.ConnectionString;
        await using var db = NewContext();
        // A version-scoped acceptance test: migrate the complete chain through AP-05.
        await db.Database.MigrateAsync(MigrationId);
    }

    [OneTimeTearDown]
    public async Task CleanupAsync()
    {
        if (_connectionString is null) return;
        await using var db = NewContext();
        await db.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task Ap05_Migration_Creates_Nullable_Historical_DueDate_And_Opening_Schema()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        Assert.Multiple(() =>
        {
            Assert.That(conn.State, Is.EqualTo(System.Data.ConnectionState.Open));
        });

        Assert.That(await IsNullableAsync(conn, "tbl_PurchaseInvoices", "DueDate"), Is.True,
            "Legacy purchase invoices must not be assigned a guessed DueDate.");
        Assert.That(await HasColumnAsync(conn, "tbl_PurchaseInvoices", "PaymentTermDaysSnapshot"), Is.True);
        Assert.That(await HasColumnAsync(conn, "tbl_PurchaseInvoices", "DueDateProvenance"), Is.True);
        Assert.That(await HasTableAsync(conn, "tbl_PurchaseInvoiceDueDateAudits"), Is.True);
        Assert.That(await HasTableAsync(conn, "tbl_SupplierOpeningBatches"), Is.True);
        Assert.That(await HasTableAsync(conn, "tbl_SupplierOpeningItems"), Is.True);
        Assert.That(await HasTableAsync(conn, "tbl_SupplierOpeningApplications"), Is.True);
        Assert.That(await HasIndexAsync(conn, "tbl_SupplierOpeningBatches", "UX_SupplierOpeningBatches_JournalEntryId"), Is.True);
        Assert.That(await HasIndexAsync(conn, "tbl_SupplierOpeningApplications", "UX_SupplierOpeningApplications_IdempotencyKey"), Is.True);
        Assert.That(await HasMigrationAsync(conn, MigrationId), Is.True);
    }

    private OasDbContext NewContext() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer(_connectionString!).Options);

    private static Task<bool> HasTableAsync(SqlConnection connection, string name) => ScalarBoolAsync(connection,
        "SELECT IIF(OBJECT_ID(N'dbo.' + @name, N'U') IS NULL, 0, 1)", name);

    private static Task<bool> HasColumnAsync(SqlConnection connection, string table, string column) =>
        TwoParameterBoolAsync(connection, "SELECT IIF(COL_LENGTH(N'dbo.' + @table, @column) IS NULL, 0, 1)", table, column);

    private static Task<bool> IsNullableAsync(SqlConnection connection, string table, string column) =>
        TwoParameterBoolAsync(connection,
            "SELECT ISNULL((SELECT c.is_nullable FROM sys.columns c JOIN sys.tables t ON c.object_id=t.object_id WHERE t.name=@table AND c.name=@column),0)", table, column);

    private static Task<bool> HasIndexAsync(SqlConnection connection, string table, string index) =>
        TwoParameterBoolAsync(connection,
            "SELECT IIF(EXISTS(SELECT 1 FROM sys.indexes i JOIN sys.tables t ON i.object_id=t.object_id WHERE t.name=@table AND i.name=@column),1,0)", table, index);

    private static Task<bool> HasMigrationAsync(SqlConnection connection, string id) => ScalarBoolAsync(connection,
        "SELECT IIF(EXISTS(SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId=@name),1,0)", id);

    private static async Task<bool> ScalarBoolAsync(SqlConnection connection, string query, string value)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = query;
        cmd.Parameters.AddWithValue("@name", value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 0;
    }

    private static async Task<bool> TwoParameterBoolAsync(SqlConnection connection, string query, string first, string second)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = query;
        cmd.Parameters.AddWithValue("@table", first);
        cmd.Parameters.AddWithValue("@column", second);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 0;
    }
}
