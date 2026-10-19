using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.ValueObjects;
using OAS.Infrastructure.Accounting.Persistence.Queries;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Accounting.Infrastructure;

/// <summary>
/// SA-04/SA-07 SQL Server query regression. Actual EF migrations and SQL queries;
/// intentionally not described as a complete sales/purchasing/optical E2E workflow.
/// The fixture creates and deletes only a randomly named test database.
/// </summary>
[TestFixture, Category("SalesSqlServer"), NonParallelizable]
public sealed class CustomerArBalancesSqlServerTests
{
    private string? _connection;
    private static readonly DateOnly NovemberEnd = new(2026, 11, 30);

    [OneTimeSetUp]
    public async Task Initialize()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_SALES_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_SALES_TEST_SQLSERVER to run Customer AR SQL Server regression tests.");
            return;
        }

        _connection = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_SA04_AR_{Guid.NewGuid():N}"
        }.ConnectionString;

        await using var db = Context();
        await db.Database.MigrateAsync();
        await SeedAsync(db);
    }

    [OneTimeTearDown]
    public async Task Cleanup()
    {
        if (string.IsNullOrWhiteSpace(_connection)) return;
        await using var db = Context();
        await db.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task Balances_NoJournalCustomerIsZero_AndCashLineTaggedWithCustomerIsExcluded()
    {
        await using var db = Context();
        var result = await new CustomerArReportingQueryService(db).BalancesAsync(NovemberEnd);
        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.Customers.Count, Is.EqualTo(2));

        var active = result.Customers.Single(x => x.CustomerCode == "AR-001");
        var withoutMovements = result.Customers.Single(x => x.CustomerCode == "AR-002");
        Assert.Multiple(() =>
        {
            Assert.That(active.BalanceBase, Is.EqualTo(90000m));
            Assert.That(active.DebitBase, Is.EqualTo(230000m));
            Assert.That(active.CreditBase, Is.EqualTo(140000m));
            Assert.That(withoutMovements.DebitBase, Is.Zero);
            Assert.That(withoutMovements.CreditBase, Is.Zero);
            Assert.That(withoutMovements.BalanceBase, Is.Zero);
            Assert.That(result.NetReceivableBase, Is.EqualTo(90000m));
            Assert.That(result.DebitBalancesBase, Is.EqualTo(90000m));
            Assert.That(result.CreditBalancesBase, Is.Zero);
        });
    }

    [Test]
    public async Task Balances_SearchAndPagination_DoNotChangeTotalsForMatchingCustomer()
    {
        await using var db = Context();
        var query = new CustomerArReportingQueryService(db);
        var match = await query.BalancesAsync(NovemberEnd, search: "AR-002", pageNumber: 1, pageSize: 1);
        var otherPage = await query.BalancesAsync(NovemberEnd, pageNumber: 2, pageSize: 1);
        Assert.Multiple(() =>
        {
            Assert.That(match.TotalCount, Is.EqualTo(1));
            Assert.That(match.Customers.Single().CustomerCode, Is.EqualTo("AR-002"));
            Assert.That(match.NetReceivableBase, Is.Zero);
            Assert.That(otherPage.Customers.Single().CustomerCode, Is.EqualTo("AR-002"));
            Assert.That(otherPage.NetReceivableBase, Is.EqualTo(90000m));
        });
    }

    [Test]
    public async Task Balances_AsOf_DoesNotIncludeFuturePostedJournals()
    {
        await using var db = Context();
        var query = new CustomerArReportingQueryService(db);
        var before = await query.BalancesAsync(new DateOnly(2026, 11, 5));
        var after = await query.BalancesAsync(new DateOnly(2026, 12, 31));
        Assert.Multiple(() =>
        {
            Assert.That(before.NetReceivableBase, Is.EqualTo(30000m));
            Assert.That(after.NetReceivableBase, Is.EqualTo(190000m));
        });
    }

    private OasDbContext Context() => new(
        new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer(_connection)
            .Options);

    private static async Task SeedAsync(OasDbContext db)
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 12, 31);
        var year = FiscalYear.Create(Guid.NewGuid(), "SA04-2026", "AR regression", start, end, FiscalYearStatus.Open);
        var period = FiscalPeriod.Create(Guid.NewGuid(), year.Id, 1, "Year 2026", start, end,
            FiscalPeriodStatus.Open, false, false, false);

        var account1 = NewAccount("AR-101", AccountType.Subledger);
        var account2 = NewAccount("AR-102", AccountType.Subledger);
        var cashAccount = NewAccount("AR-CASH", AccountType.Posting);
        var revenueAccount = Account.Create(Guid.NewGuid(), "AR-REV", "Sales Revenue", null,
            null, 1, AccountClass.Revenue, AccountType.Posting, NormalBalance.Credit,
            true, false, false, false, true, null);

        var first = Customer.Create(Guid.NewGuid(), "AR-001", account1.Id, PartyEntityType.Individual,
            "عميل اختبار 1", null, null, null, null, null, null, Gender.Unspecified,
            PartyContactInfo.Empty, true, 300000m, 30, null, true, null);
        var second = Customer.Create(Guid.NewGuid(), "AR-002", account2.Id, PartyEntityType.Individual,
            "عميل بدون قيود", null, null, null, null, null, null, Gender.Unspecified,
            PartyContactInfo.Empty, true, 300000m, 30, null, true, null);

        db.AddRange(year, period, account1, account2, cashAccount, revenueAccount, first, second);
        await db.SaveChangesAsync();

        var movements = new[]
        {
            (Date: new DateOnly(2026, 11, 1), Code: "SA04-OPEN", Amount: 30000m, IsDebit: true),
            (Date: new DateOnly(2026, 11, 7), Code: "SA04-SALE", Amount: 200000m, IsDebit: true),
            (Date: new DateOnly(2026, 11, 9), Code: "SA04-RETURN", Amount: 20000m, IsDebit: false),
            (Date: new DateOnly(2026, 11, 16), Code: "SA04-RECEIPT", Amount: 120000m, IsDebit: false),
            (Date: new DateOnly(2026, 12, 1), Code: "SA04-FUTURE", Amount: 100000m, IsDebit: true)
        };

        foreach (var m in movements)
        {
            var journal = JournalEntry.Create(Guid.NewGuid(), m.Code, JournalType.Automatic, m.Date,
                m.Date, period.Id, "AR balances SQL regression", "SA04Test", null, null, JournalEntryStatus.Posted);
            var otherAccountId = m.Code == "SA04-RECEIPT" ? cashAccount.Id : revenueAccount.Id;
            var ar = JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 1, account1.Id,
                m.IsDebit ? m.Amount : 0m, m.IsDebit ? 0m : m.Amount,
                "Customer AR", first.Id, null, null, null, null);
            // The cash/bank side deliberately has CustomerId too. It must NOT
            // be treated as a customer AR balance just because of that tag.
            var offset = JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 2, otherAccountId,
                m.IsDebit ? 0m : m.Amount, m.IsDebit ? m.Amount : 0m,
                "Counterpart account", first.Id, null, null, null, null);
            db.AddRange(journal, ar, offset);
        }
        await db.SaveChangesAsync();
    }

    private static Account NewAccount(string code, AccountType type) =>
        Account.Create(Guid.NewGuid(), code, code, null, null, 1,
            AccountClass.Asset, type, NormalBalance.Debit,
            true, false, false, false, true, null);
}
