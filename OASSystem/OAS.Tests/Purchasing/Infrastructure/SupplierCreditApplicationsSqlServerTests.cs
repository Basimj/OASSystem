using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Purchasing.Infrastructure;

// Opt in with OAS_PURCHASING_TEST_SQLSERVER. The fixture owns and drops its database.
[TestFixture, Category("PurchasingSqlServer"), NonParallelizable]
public sealed class SupplierCreditApplicationsSqlServerTests
{
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
            InitialCatalog = $"OAS_AP01_{Guid.NewGuid():N}"
        };
        _connection = builder.ConnectionString;
        await using var context = Context();
        await context.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task DropOwnedDatabase()
    {
        if (string.IsNullOrWhiteSpace(_connection)) return;
        await using var context = Context();
        await context.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task Ap01Migration_CreatesSupplierCreditApplicationSafetySchema()
    {
        await using var connection = new SqlConnection(_connection);
        await connection.OpenAsync();

        Assert.That(await TableExists(connection, "tbl_SupplierCreditApplications"), Is.True);
        Assert.That(await ColumnExists(connection, "tbl_SupplierCreditApplications", "AppliedSourceBaseAmount"), Is.True);
        Assert.That(await ColumnExists(connection, "tbl_SupplierCreditApplications", "AppliedTargetBaseAmount"), Is.True);
        Assert.That(await ColumnExists(connection, "tbl_SupplierCreditApplications", "AppliedInvoiceCurrencyAmount"), Is.True);
        Assert.That(await ColumnExists(connection, "tbl_SupplierCreditApplications", "RowVersion"), Is.True);
        Assert.That(await IndexExists(connection, "tbl_SupplierCreditApplications", "UX_SupplierCreditApplications_IdempotencyKey"), Is.True);
        Assert.That(await MigrationApplied(connection, "20261017040000_AddSupplierCreditApplications"), Is.True);
    }

    private OasDbContext Context() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer(_connection)
        .Options);

    private static async Task<bool> TableExists(SqlConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT CASE WHEN EXISTS
(
    SELECT 1 FROM sys.tables t
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo' AND t.name = @table
) THEN 1 ELSE 0 END;
""";
        command.Parameters.AddWithValue("@table", table);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

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
    WHERE t.name = @table AND i.name = @index
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
