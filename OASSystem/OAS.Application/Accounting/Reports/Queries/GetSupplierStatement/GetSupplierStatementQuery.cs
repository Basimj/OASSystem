using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.Accounting.Reports.Queries.GetSupplierStatement;

public sealed record GetSupplierStatementQuery(
    Guid SupplierId,
    DateOnly FromDate,
    DateOnly ToDate,
    string? Reference = null,
    string? SourceDocumentType = null,
    string? CurrencyCode = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    SortDirection SortDirection = SortDirection.Ascending)
    : IQuery<SupplierStatementDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        JournalPermissions.View,
        AccountingPermissions.Suppliers.View
    ];
}

public sealed class GetSupplierStatementQueryHandler(ISupplierApReportingQueryService reports)
    : IRequestHandler<GetSupplierStatementQuery, SupplierStatementDto>
{
    public Task<SupplierStatementDto> Handle(GetSupplierStatementQuery request, CancellationToken ct) =>
        reports.GetSupplierStatementAsync(
            new SupplierStatementFilterDto(
                request.SupplierId,
                request.FromDate,
                request.ToDate,
                request.Reference,
                request.SourceDocumentType,
                request.CurrencyCode,
                request.PageNumber,
                request.PageSize,
                request.SortBy,
                request.SortDirection),
            ct);
}
