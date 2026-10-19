using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Contracts.Purchasing.Performance;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Purchasing.Persistence.Queries;

namespace OAS.Tests.Purchasing.Infrastructure;

/// <summary>
/// SQL Server query-translation and schema smoke test for the consolidated AP-12
/// read model. Creates a uniquely named disposable database; never mutates production.
/// Does not substitute for a posted-document reconciliation acceptance test.
/// </summary>
[TestFixture, Category("AP12"), NonParallelizable]
public sealed class AP12PurchasingPerformanceSqlServerTests
{
    private string? _ownedConnection;

    [OneTimeSetUp]
    public async Task CreateDisposableDatabase()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_PURCHASING_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
            Assert.Ignore("Set OAS_PURCHASING_TEST_SQLSERVER to opt in to disposable AP12 SQL verification.");

        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_AP12_{Guid.NewGuid():N}",
            ApplicationName = "OAS-AP12-ReadModel-Disposable"
        };
        _ownedConnection = builder.ConnectionString;
        await using var db = Context();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task DeleteOnlyOwnedDatabase()
    {
        if (_ownedConnection is null) return;
        var databaseName = new SqlConnectionStringBuilder(_ownedConnection).InitialCatalog;
        if (!databaseName.StartsWith("OAS_AP12_", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to delete a non-AP12 test database.");
        await using var db = Context();
        await db.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task EmptyPostedDataset_AdvancedMetricsTranslateAndRemainZero()
    {
        await using var db = Context();
        var query = new PurchasingPerformanceQueryService(db);
        var report = await query.GetAsync(new PurchasingPerformanceFilterDto(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));

        Assert.Multiple(() =>
        {
            Assert.That(report.Summary.UninvoicedReceiptCostBase, Is.Zero);
            Assert.That(report.Summary.PurchasePriceVarianceBase, Is.Zero);
            Assert.That(report.Summary.OnTimeDeliveryRatePercent, Is.Null);
            Assert.That(report.Page.TotalCount, Is.Zero);
        });
    }

    private OasDbContext Context() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer(_ownedConnection!).Options);
}
