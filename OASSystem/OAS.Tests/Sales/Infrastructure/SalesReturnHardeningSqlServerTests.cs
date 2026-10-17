using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;
using OAS.Application.Sales.Lookups;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Generic;
using OAS.Infrastructure.Persistence.Repositories.Inventory;

namespace OAS.Tests.Sales.Infrastructure;

// Opt in with OAS_SALES_TEST_SQLSERVER. The fixture always creates and drops its own databases.
[TestFixture, Category("SalesSqlServer"), NonParallelizable]
public sealed class SalesReturnHardeningSqlServerTests
{
    private const string HardeningMigration = "20261016080000_SalesReturnAccountingAndQualityHardening";
    private const string PreviousMigration = "20261013203000_AccountingCoreHardening";

    private string _baseConnection = null!;
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

        _baseConnection = configured;
        _connection = BuildOwnedConnection(configured, $"OAS_SalesReturn15_{Guid.NewGuid():N}");

        await using var context = Context(_connection);
        await context.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task DropOwnedDatabase()
    {
        if (string.IsNullOrWhiteSpace(_connection)) return;
        await using var context = Context(_connection);
        await context.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task HardenedSchema_ContainsLineageQualityReturnPolicyAndWarehouseSalesPolicy()
    {
        await using var connection = new SqlConnection(_connection);
        await connection.OpenAsync();

        var postingRole = await ColumnExists(connection, "tbl_JournalEntryLines", "PostingRole");
        var returnCondition = await ColumnExists(connection, "tbl_SalesReturnLines", "ReturnCondition");
        var stockDisposition = await ColumnExists(connection, "tbl_SalesReturnLines", "StockDisposition");
        var inspectedAt = await ColumnExists(connection, "tbl_SalesReturnLines", "InspectedAtUtc");
        var inspectedBy = await ColumnExists(connection, "tbl_SalesReturnLines", "InspectedBy");
        var policyDate = await ColumnExists(connection, "tbl_SalesReturns", "ReturnPolicyReferenceDate");
        var policyDays = await ColumnExists(connection, "tbl_SalesReturns", "ReturnWindowDaysSnapshot");
        var policyOverride = await ColumnExists(connection, "tbl_SalesReturns", "ReturnWindowOverridden");
        var allowsSalesAllocation = await ColumnExists(connection, "tbl_Warehouses", "AllowsSalesAllocation");
        var migrationApplied = await MigrationApplied(connection, HardeningMigration);

        Assert.Multiple(() =>
        {
            Assert.That(postingRole, Is.True);
            Assert.That(returnCondition, Is.True);
            Assert.That(stockDisposition, Is.True);
            Assert.That(inspectedAt, Is.True);
            Assert.That(inspectedBy, Is.True);
            Assert.That(policyDate, Is.True);
            Assert.That(policyDays, Is.True);
            Assert.That(policyOverride, Is.True);
            Assert.That(allowsSalesAllocation, Is.True);
            Assert.That(migrationApplied, Is.True);
        });
    }

    [Test]
    public async Task DatabaseSchema_EnforcesJournalSourceUniquenessAndOptimisticConcurrency()
    {
        await using var connection = new SqlConnection(_connection);
        await connection.OpenAsync();

        var sourceUnique = await UniqueIndexExists(connection, "tbl_JournalEntries", "UX_JournalEntries_SourceDocument");
        var roleIndex = await IndexExists(connection, "tbl_JournalEntryLines", "IX_JournalEntryLines_Journal_Role");
        var journalRowVersion = await ColumnIsRowVersion(connection, "tbl_JournalEntries", "RowVersion");
        var returnRowVersion = await ColumnIsRowVersion(connection, "tbl_SalesReturns", "RowVersion");
        var returnLineRowVersion = await ColumnIsRowVersion(connection, "tbl_SalesReturnLines", "RowVersion");

        Assert.Multiple(() =>
        {
            Assert.That(
                sourceUnique,
                Is.True,
                "Concurrent retries must not be able to create two accounting journals for the same source document.");
            Assert.That(roleIndex, Is.True);
            Assert.That(journalRowVersion, Is.True);
            Assert.That(returnRowVersion, Is.True);
            Assert.That(returnLineRowVersion, Is.True);
        });
    }

    [Test]
    public async Task DefaultSalesWarehouseLookup_ExcludesQuarantine_ButReturnLookupCanIncludeIt()
    {
        await using var context = Context(_connection);
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

    [Test]
    public async Task HardeningMigration_CanRollbackAndReapply_OnFreshDatabase()
    {
        var recoveryConnection = BuildOwnedConnection(
            _baseConnection,
            $"OAS_SalesReturn15_Recovery_{Guid.NewGuid():N}");

        try
        {
            await using var context = Context(recoveryConnection);
            await context.Database.MigrateAsync();

            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);

            await using (var rolledBack = new SqlConnection(recoveryConnection))
            {
                await rolledBack.OpenAsync();
                Assert.That(await MigrationApplied(rolledBack, HardeningMigration), Is.False);
                Assert.That(await ColumnExists(rolledBack, "tbl_JournalEntryLines", "PostingRole"), Is.False);
                Assert.That(await ColumnExists(rolledBack, "tbl_SalesReturnLines", "ReturnCondition"), Is.False);
                Assert.That(await ColumnExists(rolledBack, "tbl_Warehouses", "AllowsSalesAllocation"), Is.False);
            }

            await migrator.MigrateAsync(HardeningMigration);

            await using var reapplied = new SqlConnection(recoveryConnection);
            await reapplied.OpenAsync();
            Assert.That(await MigrationApplied(reapplied, HardeningMigration), Is.True);
            Assert.That(await ColumnExists(reapplied, "tbl_JournalEntryLines", "PostingRole"), Is.True);
            Assert.That(await ColumnExists(reapplied, "tbl_SalesReturnLines", "ReturnCondition"), Is.True);
            Assert.That(await ColumnExists(reapplied, "tbl_Warehouses", "AllowsSalesAllocation"), Is.True);
        }
        finally
        {
            await using var cleanup = Context(recoveryConnection);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static string BuildOwnedConnection(string sourceConnection, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(sourceConnection)
        {
            InitialCatalog = databaseName
        };
        return builder.ConnectionString;
    }

    private static OasDbContext Context(string connection) => new(
        new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer(connection)
            .Options);

    private static async Task<bool> MigrationApplied(SqlConnection connection, string migrationId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = @id) THEN 1 ELSE 0 END;";
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

    private static async Task<bool> ColumnIsRowVersion(SqlConnection connection, string table, string column)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM sys.columns c
    JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = @table
      AND c.name = @column
      AND c.system_type_id = 189
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
    SELECT 1
    FROM sys.indexes i
    JOIN sys.tables t ON t.object_id = i.object_id
    WHERE t.name = @table AND i.name = @index
) THEN 1 ELSE 0 END;
""";
        command.Parameters.AddWithValue("@table", table);
        command.Parameters.AddWithValue("@index", index);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    private static async Task<bool> UniqueIndexExists(SqlConnection connection, string table, string index)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT CASE WHEN EXISTS
(
    SELECT 1
    FROM sys.indexes i
    JOIN sys.tables t ON t.object_id = i.object_id
    WHERE t.name = @table
      AND i.name = @index
      AND i.is_unique = 1
) THEN 1 ELSE 0 END;
""";
        command.Parameters.AddWithValue("@table", table);
        command.Parameters.AddWithValue("@index", index);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }
}
