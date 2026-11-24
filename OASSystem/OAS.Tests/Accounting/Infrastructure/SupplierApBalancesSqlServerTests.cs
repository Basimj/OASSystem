using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Application.Accounting.Reports.Queries.GetSupplierDebtSnapshot;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.ValueObjects;
using OAS.Infrastructure.Accounting.Persistence.Queries;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Accounting.Infrastructure;

// Actual SQL Server regression; never run against the configured database itself.
[TestFixture, Category("PurchasingSqlServer"), NonParallelizable]
public sealed class SupplierApBalancesSqlServerTests
{
    private string? _connection;
    private static readonly DateOnly AsOf = new(2026, 11, 30);

    [OneTimeSetUp]
    public async Task Initialize()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_PURCHASING_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_PURCHASING_TEST_SQLSERVER to run AP balance regression tests.");
            return;
        }
        _connection = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_AP_BAL_{Guid.NewGuid():N}"
        }.ConnectionString;
        await using var db = Context();
        await db.Database.MigrateAsync();
        await SeedAsync(db);
    }

    [OneTimeTearDown]
    public async Task Cleanup()
    {
        if (_connection is null) return;
        await using var db = Context();
        await db.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task Balances_IncludeZero_AndExcludeFutureDraftAndCounterpartLines()
    {
        await using var db = Context();
        var result = await new SupplierApReportingQueryService(db).GetSupplierBalancesAsync(
            new SupplierBalancesFilterDto(AsOf, IncludeZeroBalance: true) { AllRows = true });
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalCount, Is.EqualTo(4));
            Assert.That(result.Rows.Count, Is.EqualTo(4));
            Assert.That(result.TotalLiabilityBaseAmount, Is.EqualTo(70m));
            Assert.That(result.TotalCreditBalanceBaseAmount, Is.EqualTo(100m));
            Assert.That(result.TotalDebitBalanceBaseAmount, Is.EqualTo(30m));
            var creditor = result.Rows.Single(x => x.SupplierCode == "AP-T01");
            Assert.That(creditor.CreditToDateBaseAmount, Is.EqualTo(140m));
            Assert.That(creditor.DebitToDateBaseAmount, Is.EqualTo(40m));
            Assert.That(creditor.LiabilityBalanceBaseAmount, Is.EqualTo(100m));
            var debtor = result.Rows.Single(x => x.SupplierCode == "AP-T02");
            Assert.That(debtor.LiabilityBalanceBaseAmount, Is.EqualTo(-30m));
            Assert.That(debtor.IsActive, Is.False);
            var empty = result.Rows.Single(x => x.SupplierCode == "AP-T03");
            Assert.That(empty.DebitToDateBaseAmount, Is.Zero);
            Assert.That(empty.CreditToDateBaseAmount, Is.Zero);
            var settled = result.Rows.Single(x => x.SupplierCode == "AP-T04");
            Assert.That(settled.DebitToDateBaseAmount, Is.EqualTo(20m));
            Assert.That(settled.CreditToDateBaseAmount, Is.EqualTo(20m));
            Assert.That(settled.LiabilityBalanceBaseAmount, Is.Zero);
        });
    }

    [Test]
    public async Task Balances_FilterSortAndPage_KeepFullFilteredTotals()
    {
        await using var db = Context();
        var service = new SupplierApReportingQueryService(db);
        var ascending = new SupplierBalancesFilterDto(AsOf, PageSize: 1, SortBy: "balance");
        var first = await service.GetSupplierBalancesAsync(ascending);
        var second = await service.GetSupplierBalancesAsync(ascending with { PageNumber = 2 });
        var desc = await service.GetSupplierBalancesAsync(ascending with { SortDirection = SortDirection.Descending });
        var search = await service.GetSupplierBalancesAsync(ascending with { Search = "AP-T02" });
        var missing = await service.GetSupplierBalancesAsync(ascending with { Search = "NO-SUPPLIER" });
        var future = await service.GetSupplierBalancesAsync(ascending with { AsOfDate = new DateOnly(2026, 12, 31) });
        Assert.Multiple(() =>
        {
            Assert.That(first.TotalCount, Is.EqualTo(2));
            Assert.That(first.Rows.Single().SupplierCode, Is.EqualTo("AP-T02"));
            Assert.That(second.Rows.Single().SupplierCode, Is.EqualTo("AP-T01"));
            Assert.That(desc.Rows.Single().SupplierCode, Is.EqualTo("AP-T01"));
            Assert.That(second.TotalLiabilityBaseAmount, Is.EqualTo(70m));
            Assert.That(search.TotalLiabilityBaseAmount, Is.EqualTo(-30m));
            Assert.That(missing.TotalCount, Is.Zero);
            Assert.That(missing.Rows, Is.Empty);
            Assert.That(missing.TotalLiabilityBaseAmount, Is.Zero);
            Assert.That(future.TotalLiabilityBaseAmount, Is.EqualTo(120m));
        });
    }

    [Test]
    public async Task Snapshot_ExecutesRealBalancesAndAgingQueries()
    {
        await using var db = Context();
        var result = await new GetSupplierDebtSnapshotQueryHandler(
            new SupplierApReportingQueryService(db), new SupplierAgingQueryService(db))
            .Handle(new GetSupplierDebtSnapshotQuery(AsOf), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Suppliers.Count, Is.EqualTo(4));
            Assert.That(result.Suppliers.Sum(x => x.LiabilityBalanceBaseAmount), Is.EqualTo(70m));
            // Seeded posted journals only; no purchase or opening-item documents.
            Assert.That(result.Documents, Is.Empty);
        });
    }

    private OasDbContext Context() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer(_connection!).Options);

    private static async Task SeedAsync(OasDbContext db)
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 12, 31);
        var year = FiscalYear.Create(Guid.NewGuid(), "AP-TEST-26", "AP regression", start, end, FiscalYearStatus.Open);
        var period = FiscalPeriod.Create(Guid.NewGuid(), year.Id, 1, "2026", start, end,
            FiscalPeriodStatus.Open, false, false, false);
        var accounts = Enumerable.Range(1, 4).Select(i => Account.Create(Guid.NewGuid(),
            $"AP-A0{i}", $"AP account {i}", null, null, 1, AccountClass.Liability,
            AccountType.Subledger, NormalBalance.Credit, true, false, false, false, true, null)).ToArray();
        var offset = Account.Create(Guid.NewGuid(), "AP-OFFSET", "Offset", null, null, 1,
            AccountClass.Asset, AccountType.Posting, NormalBalance.Debit,
            true, false, false, false, true, null);
        var suppliers = accounts.Select((a, i) => Supplier.Create(Guid.NewGuid(), $"AP-T0{i + 1}", a.Id,
            PartyEntityType.Individual, SupplierScope.Local, $"مورد اختبار {i + 1}",
            null, null, null, null, null, PartyContactInfo.Empty, 0m, 30, null, null, i != 1, null)).ToArray();
        db.AddRange(year, period, offset);
        db.AddRange(accounts);
        db.AddRange(suppliers);
        await db.SaveChangesAsync();

        void Add(int index, string code, DateOnly date, decimal debit, decimal credit, JournalEntryStatus status)
        {
            var journal = JournalEntry.Create(Guid.NewGuid(), code, JournalType.Manual, date, date,
                period.Id, "AP regression", "APTest", null, null, status);
            db.AddRange(journal,
                JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 1, accounts[index].Id,
                    debit, credit, "AP", null, suppliers[index].Id, null, null, null),
                JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 2, offset.Id,
                    credit, debit, "Offset", null, suppliers[index].Id, null, null, null));
        }
        var date = new DateOnly(2026, 11, 1);
        Add(0, "AP-J1", date, 0m, 140m, JournalEntryStatus.Posted);
        Add(0, "AP-J2", date, 40m, 0m, JournalEntryStatus.Posted);
        Add(1, "AP-J3", date, 30m, 0m, JournalEntryStatus.Posted);
        Add(3, "AP-J4", date, 0m, 20m, JournalEntryStatus.Posted);
        Add(3, "AP-J5", date, 20m, 0m, JournalEntryStatus.Posted);
        Add(0, "AP-J6", new DateOnly(2026, 12, 1), 0m, 50m, JournalEntryStatus.Posted);
        Add(0, "AP-J7", date, 0m, 999m, JournalEntryStatus.Draft);
        await db.SaveChangesAsync();
    }
}
