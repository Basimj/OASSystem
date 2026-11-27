using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Infrastructure.Accounting.Persistence.Queries;
using OAS.Infrastructure.Sales.Persistence.Queries;
using OAS.Contracts.Sales.Reports;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Sales.Infrastructure;

/// <summary>
/// SA-07 read-only checks on a populated SQL Server TEST/REPLICA database.
/// No insert, migration or DROP is performed by this fixture.
/// This is not a substitute for a full Sales / Purchasing / Optical HTTP E2E test.
/// </summary>
[TestFixture, Category("SalesAcceptanceSqlServer"), NonParallelizable]
public sealed class SalesArFinancialInvariantsSqlServerTests
{
    private string _connectionString = null!;

    [OneTimeSetUp]
    public async Task Initialize()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_SALES_ACCEPTANCE_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_SALES_ACCEPTANCE_SQLSERVER to a populated disposable SQL Server TEST/REPLICA database. No data is modified.");
            return;
        }
        _connectionString = new SqlConnectionStringBuilder(configured)
        {
            ApplicationName = "OAS-SA07-ReadOnly-Acceptance", ConnectTimeout = 15
        }.ConnectionString;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID(N'dbo.tbl_JournalEntries') IS NOT NULL AND OBJECT_ID(N'dbo.tbl_JournalEntryLines') IS NOT NULL AND OBJECT_ID(N'dbo.tbl_Customers') IS NOT NULL THEN 1 ELSE 0 END", connection);
        Assert.That(Convert.ToInt32(await cmd.ExecuteScalarAsync()), Is.EqualTo(1));
    }

    [Test]
    public async Task SA07_PostedJournals_AreBalancedAndHaveAtLeastTwoLines()
    {
        var invalid = await CountAsync("""
            SELECT COUNT_BIG(*) FROM (
                SELECT j.Id FROM dbo.tbl_JournalEntries j
                LEFT JOIN dbo.tbl_JournalEntryLines l ON l.JournalEntryId=j.Id
                WHERE j.Status=4
                GROUP BY j.Id
                HAVING COUNT_BIG(l.Id)<2 OR COALESCE(SUM(l.DebitAmount-l.CreditAmount),0)<>0
            ) x;
            """);
        Assert.That(invalid, Is.Zero, "Every posted accounting journal must balance.");
    }

    [Test]
    public async Task SA07_NoDuplicatePostedJournalSource()
    {
        var invalid = await CountAsync("""
            SELECT COUNT_BIG(*) FROM (
                SELECT SourceModule,SourceDocumentType,SourceDocumentId
                FROM dbo.tbl_JournalEntries
                WHERE SourceModule IS NOT NULL AND SourceDocumentType IS NOT NULL AND SourceDocumentId IS NOT NULL
                GROUP BY SourceModule,SourceDocumentType,SourceDocumentId HAVING COUNT_BIG(*)>1
            ) x;
            """);
        Assert.That(invalid, Is.Zero, "Re-posting a source must not create another journal.");
    }

    [Test]
    public async Task SA07_PostedSalesInvoices_ReferencePostedJournal()
    {
        var invalid = await CountAsync("""
            SELECT COUNT_BIG(*) FROM dbo.tbl_SalesInvoices i
            LEFT JOIN dbo.tbl_JournalEntries j ON j.Id=i.JournalEntryId
            WHERE i.Status=3 AND (i.JournalEntryId IS NULL OR j.Id IS NULL OR j.Status<>4);
            """);
        Assert.That(invalid, Is.Zero, "Every posted sale must have a posted accounting journal.");
    }

    [Test]
    public async Task SA07_PostedSalesReturns_ReferencePostedJournal()
    {
        var invalid = await CountAsync("""
            SELECT COUNT_BIG(*) FROM dbo.tbl_SalesReturns r
            LEFT JOIN dbo.tbl_JournalEntries j ON j.Id=r.JournalEntryId
            WHERE r.Status=3 AND (r.JournalEntryId IS NULL OR j.Id IS NULL OR j.Status<>4);
            """);
        Assert.That(invalid, Is.Zero, "Every posted return must have a posted accounting journal.");
    }

    [Test]
    public async Task SA07_CustomerBalances_EqualIndependentPostedGLQuery()
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        decimal expected;
        await using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            await using var cmd = new SqlCommand("""
                SELECT COALESCE(SUM(l.DebitAmount-l.CreditAmount),0)
                FROM dbo.tbl_Customers c
                JOIN dbo.tbl_Accounts a ON a.Id=c.AccountId
                JOIN dbo.tbl_JournalEntryLines l ON l.AccountId=a.Id
                JOIN dbo.tbl_JournalEntries j ON j.Id=l.JournalEntryId
                WHERE j.Status=4 AND j.PostingDate<=@asOf;
                """, connection);
            cmd.Parameters.Add(new SqlParameter("@asOf", System.Data.SqlDbType.Date)
                { Value = asOf.ToDateTime(TimeOnly.MinValue) });
            expected = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
        }
        await using var db = new OasDbContext(new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer(_connectionString).Options);
        var actual = await new CustomerArReportingQueryService(db).BalancesAsync(asOf);
        Assert.That(actual.NetReceivableBase, Is.EqualTo(expected).Within(0.0001m),
            "Customer report must reflect customer-account posted GL lines, not cash/bank CustomerId tags.");
    }

    [Test]
    public async Task SalesFilterOptions_AreIndependentAndPaged()
    {
        await using var db = new OasDbContext(new DbContextOptionsBuilder<OasDbContext>().UseSqlServer(_connectionString).Options);
        var service = new SalesReportQueryService(db);
        var from = new DateOnly(2000, 1, 1);
        var through = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var kind in new[] { "historicalBaseCurrency", "customer", "employee", "product", "category", "type", "brand", "warehouse", "operator", "currency" })
        {
            var request = new SalesReportOptionRequest(from, through, kind, PageSize: 1);
            var first = await service.GetOptionsAsync(request);
            var second = await service.GetOptionsAsync(request with { PageNumber = 2 });
            Assert.That(first.Items.Count, Is.LessThanOrEqualTo(1), kind);
            Assert.That(second.Items.Count, Is.LessThanOrEqualTo(1), kind);
            Assert.That(first.TotalCount, Is.GreaterThanOrEqualTo(first.Items.Count), kind);
            if (first.Items.Count == 1 && second.Items.Count == 1)
                Assert.That(second.Items[0].Key, Is.Not.EqualTo(first.Items[0].Key), kind);
            if (first.Items.Count == 1)
            {
                var matching = await service.GetOptionsAsync(request with { Search = first.Items[0].Label, PageSize = 100 });
                Assert.That(matching.TotalCount, Is.GreaterThanOrEqualTo(1), kind);
            }
        }
    }

    [Test]
    public async Task SalesFilterOptions_RemainAvailableWhenReportExceedsLimit()
    {
        await using var db = new OasDbContext(new DbContextOptionsBuilder<OasDbContext>().UseSqlServer(_connectionString).Options);
        var service = new SalesReportQueryService(db);
        var from = new DateOnly(2000, 1, 1);
        var through = DateOnly.FromDateTime(DateTime.UtcNow);
        // The opt-in test database must contain a period exceeding one of the report guards.
        try
        {
            await service.GetAsync(new SalesReportFilterDto(from, through, SalesReportGroupBy.Line,
                HistoricalBaseCurrencyCode: "*", IncludeFilterOptions: false));
            Assert.Ignore("Use a test dataset with more than 100,000 scoped lines or 50,000 breakdown rows.");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("100,000") || ex.Message.Contains("50,000"))
        {
            var options = await service.GetOptionsAsync(new SalesReportOptionRequest(from, through, "customer", PageSize: 10));
            Assert.That(options.Items, Is.Not.Empty);
            Assert.That(options.Items.Count, Is.LessThanOrEqualTo(10));
        }
    }

    [Test]
    public async Task GeneralLedger_CostCenterMatchesPostedJournalLines()
    {
        await using var db = new OasDbContext(new DbContextOptionsBuilder<OasDbContext>().UseSqlServer(_connectionString).Options);
        var fromDate = new DateOnly(2000, 1, 1);
        var throughDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var entries = from l in db.Set<OAS.Domain.Accounting.Entities.JournalEntryLine>().AsNoTracking()
                      join j in db.Set<OAS.Domain.Accounting.Entities.JournalEntry>().AsNoTracking() on l.JournalEntryId equals j.Id
                      where l.CostCenterId.HasValue && j.Status == OAS.Domain.Accounting.Enums.JournalEntryStatus.Posted && j.PostingDate >= fromDate && j.PostingDate <= throughDate
                      select new { l.AccountId, l.CostCenterId, l.DebitAmount, l.CreditAmount };
        var selected = await entries.FirstOrDefaultAsync();
        if (selected is null) { Assert.Ignore("Populate the test database with posted cost-center lines."); return; }
        var scoped = entries.Where(x => x.AccountId == selected.AccountId && x.CostCenterId == selected.CostCenterId);
        var debit = await scoped.SumAsync(x => x.DebitAmount);
        var credit = await scoped.SumAsync(x => x.CreditAmount);
        var report = await new AccountingReportingQueryService(db, TimeProvider.System).GetGeneralLedgerAsync(
            new OAS.Contracts.Accounting.Reports.GeneralLedgerFilterDto(fromDate, throughDate, AccountId: selected.AccountId, CostCenterId: selected.CostCenterId));
        Assert.That(report.TotalDebit, Is.EqualTo(debit));
        Assert.That(report.TotalCredit, Is.EqualTo(credit));
    }

    [Test]
    public async Task SA06_SalesReport_TotalsMatchHistoricalPostedDocumentLines()
    {
        var from = new DateOnly(2000, 1, 1);
        var through = DateOnly.FromDateTime(DateTime.UtcNow);
        await using var db = new OasDbContext(new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer(_connectionString).Options);
        var report = await new SalesReportQueryService(db).GetAsync(
            new SalesReportFilterDto(from, through, SalesReportGroupBy.Day));

        decimal invoiced;
        decimal returned;
        await using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            await using (var command = new SqlCommand("""
                SELECT COALESCE(SUM(i.BaseTotalAmount-i.BaseTaxAmount),0)
                FROM dbo.tbl_SalesInvoices i
                WHERE i.Status=3 AND i.PostingDate>=@start AND i.PostingDate<=@through
                  AND EXISTS (SELECT 1 FROM dbo.tbl_SalesInvoiceLines l WHERE l.SalesInvoiceId=i.Id AND l.IsActive=1);
                """, connection))
            {
                command.Parameters.Add(new SqlParameter("@start", System.Data.SqlDbType.Date)
                    { Value = from.ToDateTime(TimeOnly.MinValue) });
                command.Parameters.Add(new SqlParameter("@through", System.Data.SqlDbType.Date)
                    { Value = through.ToDateTime(TimeOnly.MinValue) });
                invoiced = Convert.ToDecimal(await command.ExecuteScalarAsync());
            }
            await using (var command = new SqlCommand("""
                SELECT COALESCE(SUM(r.BaseNetAmount),0)
                FROM dbo.tbl_SalesReturns r
                WHERE r.Status=3 AND r.PostingDate>=@start AND r.PostingDate<=@through
                  AND EXISTS (SELECT 1 FROM dbo.tbl_SalesReturnLines l WHERE l.SalesReturnId=r.Id AND l.IsActive=1);
                """, connection))
            {
                command.Parameters.Add(new SqlParameter("@start", System.Data.SqlDbType.Date)
                    { Value = from.ToDateTime(TimeOnly.MinValue) });
                command.Parameters.Add(new SqlParameter("@through", System.Data.SqlDbType.Date)
                    { Value = through.ToDateTime(TimeOnly.MinValue) });
                returned = Convert.ToDecimal(await command.ExecuteScalarAsync());
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(report.Totals.InvoiceRevenueBase, Is.EqualTo(invoiced).Within(0.0001m));
            Assert.That(report.Totals.ReturnedRevenueBase, Is.EqualTo(returned).Within(0.0001m));
            Assert.That(report.Totals.NetRevenueBase, Is.EqualTo(invoiced-returned).Within(0.0001m));
        });
    }

    private async Task<long> CountAsync(string sql)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
