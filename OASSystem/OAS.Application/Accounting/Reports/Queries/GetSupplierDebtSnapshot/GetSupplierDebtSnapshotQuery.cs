using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetSupplierDebtSnapshot;

public sealed record GetSupplierDebtSnapshotQuery(DateOnly AsOfDate)
    : IQuery<SupplierDebtSnapshotDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [JournalPermissions.View, AccountingPermissions.Suppliers.View];
}

public sealed class GetSupplierDebtSnapshotQueryHandler(
    ISupplierApReportingQueryService balances, ISupplierAgingQueryService aging)
    : IRequestHandler<GetSupplierDebtSnapshotQuery, SupplierDebtSnapshotDto>
{
    public async Task<SupplierDebtSnapshotDto> Handle(GetSupplierDebtSnapshotQuery request, CancellationToken ct)
    {
        // Execute sequentially: the two services may share one scoped DbContext.
        var ledger = await balances.GetSupplierBalancesAsync(
            new SupplierBalancesFilterDto(request.AsOfDate, IncludeZeroBalance: true) { AllRows = true }, ct);
        var open = await aging.GetAsync(new SupplierAgingFilterDto(request.AsOfDate) { AllRows = true }, ct);
        return new SupplierDebtSnapshotDto(request.AsOfDate, ledger.Rows, open.Lines);
    }
}
