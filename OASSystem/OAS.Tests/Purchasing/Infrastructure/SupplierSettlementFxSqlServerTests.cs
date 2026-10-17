using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Purchasing.Infrastructure;

// Opt in with OAS_PURCHASING_TEST_SQLSERVER. The fixture owns and drops its database.
[TestFixture, Category("PurchasingSqlServer"), NonParallelizable]
public sealed class SupplierSettlementFxSqlServerTests
{
    private const string Ap02MigrationId = "20261017150000_AddSupplierSettlementFxSnapshots";
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
            InitialCatalog = $"OAS_AP02_{Guid.NewGuid():N}"
        };
        _connection = builder.ConnectionString;
        await using var context = Context();
        await context.Database.MigrateAsync(Ap02MigrationId);
    }

    [OneTimeTearDown]
    public async Task DropOwnedDatabase()
    {
        if (string.IsNullOrWhiteSpace(_connection)) return;
        await using var context = Context();
        await context.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task Ap02Migration_CreatesSupplierSettlementFxSafetySchema()
    {
        await using var connection = new SqlConnection(_connection);
        await connection.OpenAsync();

        Assert.Multiple(() =>
        {
            Assert.That(ColumnExists(connection, "tbl_PaymentAllocations", "TargetDocumentAmount").GetAwaiter().GetResult(), Is.True);
            Assert.That(CheckConstraintExists(connection, "tbl_PaymentAllocations", "CK_PaymentAllocations_TargetDocumentAmount_Positive").GetAwaiter().GetResult(), Is.True);
            Assert.That(IndexExists(connection, "tbl_PaymentAllocations", "IX_PaymentAllocations_TargetDocument_Date").GetAwaiter().GetResult(), Is.True);

            Assert.That(ColumnExists(connection, "tbl_PurchaseInvoices", "CurrencyCodeSnapshot").GetAwaiter().GetResult(), Is.True);
            Assert.That(ColumnExists(connection, "tbl_PurchaseInvoices", "CurrencyDecimalPlacesSnapshot").GetAwaiter().GetResult(), Is.True);
            Assert.That(ColumnExists(connection, "tbl_PurchaseInvoices", "BaseCurrencyId").GetAwaiter().GetResult(), Is.True);
            Assert.That(ColumnExists(connection, "tbl_PurchaseInvoices", "BaseCurrencyCodeSnapshot").GetAwaiter().GetResult(), Is.True);
            Assert.That(ColumnExists(connection, "tbl_PurchaseInvoices", "BaseCurrencyDecimalPlacesSnapshot").GetAwaiter().GetResult(), Is.True);
            Assert.That(ColumnExists(connection, "tbl_PurchaseInvoices", "ExchangeRateType").GetAwaiter().GetResult(), Is.True);
            Assert.That(ColumnExists(connection, "tbl_PurchaseInvoices", "ExchangeRateSource").GetAwaiter().GetResult(), Is.True);
            Assert.That(IndexExists(connection, "tbl_PurchaseInvoices", "IX_PurchaseInvoices_BaseCurrencyId").GetAwaiter().GetResult(), Is.True);
            Assert.That(ForeignKeyExists(connection, "tbl_PurchaseInvoices", "FK_PurchaseInvoices_Currencies_BaseCurrencyId").GetAwaiter().GetResult(), Is.True);
            Assert.That(MigrationApplied(connection, Ap02MigrationId).GetAwaiter().GetResult(), Is.True);
        });
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

    private static async Task<bool> CheckConstraintExists(SqlConnection connection, string table, string constraint)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT CASE WHEN EXISTS
(
    SELECT 1 FROM sys.check_constraints cc
    JOIN sys.tables t ON t.object_id = cc.parent_object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo' AND t.name = @table AND cc.name = @constraint
) THEN 1 ELSE 0 END;
""";
        command.Parameters.AddWithValue("@table", table);
        command.Parameters.AddWithValue("@constraint", constraint);
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

    private static async Task<bool> ForeignKeyExists(SqlConnection connection, string table, string foreignKey)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT CASE WHEN EXISTS
(
    SELECT 1 FROM sys.foreign_keys fk
    JOIN sys.tables t ON t.object_id = fk.parent_object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo' AND t.name = @table AND fk.name = @foreignKey
) THEN 1 ELSE 0 END;
""";
        command.Parameters.AddWithValue("@table", table);
        command.Parameters.AddWithValue("@foreignKey", foreignKey);
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
