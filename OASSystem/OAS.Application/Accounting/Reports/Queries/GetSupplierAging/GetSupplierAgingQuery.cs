using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetSupplierAging;

public sealed record GetSupplierAgingQuery(SupplierAgingFilterDto Filter)
    : IQuery<SupplierAgingReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Journals.View, AccountingPermissions.Suppliers.View];
}

public sealed class GetSupplierAgingQueryHandler(ISupplierAgingQueryService aging)
    : IRequestHandler<GetSupplierAgingQuery, SupplierAgingReportDto>
{
    public Task<SupplierAgingReportDto> Handle(GetSupplierAgingQuery request, CancellationToken cancellationToken) =>
        aging.GetAsync(request.Filter, cancellationToken);
}
