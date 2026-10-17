using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Accounting.Reports.Queries.GetSupplierBalances;

public sealed record GetSupplierBalancesQuery(
    DateOnly AsOfDate,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    SortDirection SortDirection = SortDirection.Ascending,
    bool IncludeZeroBalance = false)
    : IQuery<SupplierBalancesDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        JournalPermissions.View,
        AccountingPermissions.Suppliers.View
    ];
}

public sealed class GetSupplierBalancesQueryHandler(ISupplierApReportingQueryService reports)
    : IRequestHandler<GetSupplierBalancesQuery, SupplierBalancesDto>
{
    public Task<SupplierBalancesDto> Handle(GetSupplierBalancesQuery request, CancellationToken ct) =>
        reports.GetSupplierBalancesAsync(
            new SupplierBalancesFilterDto(
                request.AsOfDate,
                request.Search,
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.SortDirection,
                request.IncludeZeroBalance),
            ct);
}
