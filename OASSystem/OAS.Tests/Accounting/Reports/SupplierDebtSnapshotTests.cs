using NUnit.Framework;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Reports.Queries.GetSupplierDebtSnapshot;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Tests.Accounting.Reports;

[TestFixture]
public sealed class SupplierDebtSnapshotTests
{
    [Test]
    public async Task Snapshot_Requests_All_Rows_At_Same_Date_And_Preserves_Liability_And_Open_Documents()
    {
        var date = new DateOnly(2026, 11, 24);
        var id = Guid.NewGuid();
        var rows = Enumerable.Range(1, 105).Select(i => new SupplierBalanceRowDto(
            i == 1 ? id : Guid.NewGuid(), $"SUP-{i}", $"Supplier {i}", Guid.NewGuid(), $"210{i}", "AP",
            50m, 150m, 100m, 0m, 100m) { IsActive = i != 1 }).ToArray();
        var document = new SupplierAgingLineDto("PurchaseInvoice", Guid.NewGuid(), "PINV-1", id,
            "SUP-1", "Supplier 1", date.AddDays(-20), date.AddDays(-20), date.AddDays(-1), "Explicit", false, 1, "0-30",
            200m, 110m, "YER", 200m) { PaymentBaseAmount = 50m, CreditAppliedBaseAmount = 40m };
        var ledger = new LedgerStub(rows);
        var aging = new AgingStub(document);
        var result = await new GetSupplierDebtSnapshotQueryHandler(ledger, aging)
            .Handle(new GetSupplierDebtSnapshotQuery(date), CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(ledger.Filter!.AllRows, Is.True);
            Assert.That(ledger.Filter.IncludeZeroBalance, Is.True);
            Assert.That(aging.Filter!.AllRows, Is.True);
            Assert.That(aging.Filter.AsOfDate, Is.EqualTo(ledger.Filter.AsOfDate));
            Assert.That(result.AsOf, Is.EqualTo(date));
            Assert.That(result.Suppliers, Has.Count.EqualTo(105));
            Assert.That(result.Suppliers[0].IsActive, Is.False);
            Assert.That(result.Suppliers[0].LiabilityBalanceBaseAmount, Is.EqualTo(100m));
            Assert.That(result.Documents[0].OutstandingBaseAmount, Is.EqualTo(110m));
            Assert.That(result.Documents[0].PaymentBaseAmount, Is.EqualTo(50m));
            Assert.That(result.Documents[0].CreditAppliedBaseAmount, Is.EqualTo(40m));
        });
    }

    [Test]
    public void Snapshot_Requires_Journal_And_Supplier_View_Permissions()
    {
        var query = new GetSupplierDebtSnapshotQuery(new DateOnly(2026, 11, 24));
        Assert.That(query.RequiredPermissions, Does.Contain("accounting.journals.view"));
        Assert.That(query.RequiredPermissions, Does.Contain("accounting.suppliers.view"));
    }

    private sealed class LedgerStub(SupplierBalanceRowDto[] rows) : ISupplierApReportingQueryService
    {
        public SupplierBalancesFilterDto? Filter { get; private set; }
        public Task<SupplierBalancesDto> GetSupplierBalancesAsync(SupplierBalancesFilterDto filter, CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return Task.FromResult(new SupplierBalancesDto(filter.AsOfDate, rows, 10500m, 0m, 10500m, rows.Length, 1, 50));
        }
        public Task<SupplierStatementDto> GetSupplierStatementAsync(SupplierStatementFilterDto filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ApReconciliationDto> GetApReconciliationAsync(DateOnly asOfDate, Guid? controlAccountId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class AgingStub(SupplierAgingLineDto document) : ISupplierAgingQueryService
    {
        public SupplierAgingFilterDto? Filter { get; private set; }
        public Task<SupplierAgingReportDto> GetAsync(SupplierAgingFilterDto filter, CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return Task.FromResult(new SupplierAgingReportDto(filter.AsOfDate, [document], [], 0m, 110m, 0m, 0m, 0m, 0m, 110m, 0m, 0m, 1, 1, 100));
        }
    }
}
