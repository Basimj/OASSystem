using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Sales.Lookups;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Generic;
using OAS.Infrastructure.Persistence.Repositories.Inventory;

namespace OAS.Tests.Sales.Infrastructure;

// Opt in with OAS_SALES_TEST_SQLSERVER. The fixture always creates and drops its own database.
[TestFixture, Category("SalesSqlServer"), NonParallelizable]
public sealed class SalesReturnHardeningSqlServerTests
{
    private string _connection = null!;

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_SALES_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_SALES_TEST_SQLSERVER to run Sales return SQL Server tests.");
            return;
        }

        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_SalesReturn15_{Guid.NewGuid():N}"
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
    public async Task HardenedSchema_ContainsLineageQualityAndWarehouseSalesPolicy()
    {
        await using var connection = new SqlConnection(_connection);
        await connection.OpenAsync();

        Assert.That(await ColumnExists(connection, "tbl_JournalEntryLines", "PostingRole"), Is.True);
        Assert.That(await ColumnExists(connection, "tbl_SalesReturnLines", "ReturnCondition"), Is.True);
        Assert.That(await ColumnExists(connection, "tbl_SalesReturnLines", "StockDisposition"), Is.True);
        Assert.That(await ColumnExists(connection, "tbl_SalesReturnLines", "InspectedAtUtc"), Is.True);
        Assert.That(await ColumnExists(connection, "tbl_Warehouses", "AllowsSalesAllocation"), Is.True);
        Assert.That(await MigrationApplied(connection, "20261016080000_SalesReturnAccountingAndQualityHardening"), Is.True);
    }

    [Test]
    public async Task DefaultSalesWarehouseLookup_ExcludesQuarantine_ButReturnLookupCanIncludeIt()
    {
        await using var context = Context();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var sellable = new Warehouse($"S-{suffix}", "Sellable", allowsSalesAllocation: true);
        var quarantine = new Warehouse($"Q-{suffix}", "Quarantine", allowsSalesAllocation: false);
        context.AddRange(sellable, quarantine);
        await context.SaveChangesAsync();

        var handler = new SearchSalesWarehousesQueryHandler(
            new EfRepository<Warehouse, Guid>(context),
            new InventoryBalanceRepository(context));

        var normal = await handler.Handle(new SearchSalesWarehousesQuery(null, suffix, 20, false), CancellationToken.None);
        var returns = await handler.Handle(new SearchSalesWarehousesQuery(null, suffix, 20, true), CancellationToken.None);

        Assert.That(normal.Any(x => x.Id == sellable.Id), Is.True);
        Assert.That(normal.Any(x => x.Id == quarantine.Id), Is.False);
        Assert.That(returns.Any(x => x.Id == sellable.Id), Is.True);
        Assert.That(returns.Any(x => x.Id == quarantine.Id), Is.True);
        Assert.That(returns.Single(x => x.Id == quarantine.Id).AllowsSalesAllocation, Is.False);
    }

    private OasDbContext Context() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer(_connection)
        .Options);

    private static async Task<bool> MigrationApplied(SqlConnection connection, string migrationId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = @id) THEN 1 ELSE 0 END;";
        command.Parameters.AddWithValue("@id", migrationId);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task<bool> ColumnExists(SqlConnection connection, string table, string column)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM sys.columns c
    JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = @table AND c.name = @column
) THEN 1 ELSE 0 END;
""";
        command.Parameters.AddWithValue("@table", table);
        command.Parameters.AddWithValue("@column", column);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }
}
