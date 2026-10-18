using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;
using OAS.Application.Purchasing.Authorization;

namespace OAS.Application.Accounting.Reports.Queries.GetSupplierWorkspaceDocuments;

public sealed record GetSupplierWorkspaceDocumentsQuery(SupplierWorkspaceDocumentsFilter Filter)
    : IQuery<SupplierWorkspaceDocumentsDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Suppliers.View, JournalPermissions.View, PurchasingPermissions.View];
}

public sealed class GetSupplierWorkspaceDocumentsQueryHandler(ISupplierWorkspaceDocumentsService documents)
    : IRequestHandler<GetSupplierWorkspaceDocumentsQuery, SupplierWorkspaceDocumentsDto>
{
    public Task<SupplierWorkspaceDocumentsDto> Handle(GetSupplierWorkspaceDocumentsQuery query, CancellationToken ct) =>
        documents.GetAsync(query.Filter, ct);
}
