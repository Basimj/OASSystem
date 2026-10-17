using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetApReconciliation;

public sealed record GetApReconciliationQuery(
    DateOnly AsOfDate,
    Guid? ControlAccountId = null)
    : IQuery<ApReconciliationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        JournalPermissions.View,
        AccountingPermissions.Suppliers.View
    ];
}

public sealed class GetApReconciliationQueryHandler(ISupplierApReportingQueryService reports)
    : IRequestHandler<GetApReconciliationQuery, ApReconciliationDto>
{
    public Task<ApReconciliationDto> Handle(GetApReconciliationQuery request, CancellationToken ct) =>
        reports.GetApReconciliationAsync(request.AsOfDate, request.ControlAccountId, ct);
}
