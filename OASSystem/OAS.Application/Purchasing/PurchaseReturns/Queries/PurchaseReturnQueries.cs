using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.PurchaseReturns;

namespace OAS.Application.Purchasing.PurchaseReturns.Queries;

public sealed record GetPurchaseReturnByIdQuery(Guid Id) : IQuery<PurchaseReturnDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Receipts.View];
}
public sealed record GetPurchaseReturnsQuery(
    PageRequest Request,
    PurchaseReturnStatus? Status = null,
    Guid? PurchaseReceiptId = null,
    Guid? PurchaseInvoiceId = null,
    Guid? SupplierId = null) : IQuery<PagedResult<PurchaseReturnDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Receipts.View];
}
