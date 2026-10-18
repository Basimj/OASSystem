using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.PurchaseOrders;

namespace OAS.Application.Purchasing.PurchaseOrders.Queries;

public sealed record GetPurchaseOrderProgressQuery(Guid OrderId)
    : IQuery<PurchaseOrderProgressDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [PurchasingPermissions.Orders.View, PurchasingPermissions.Receipts.View, PurchasingPermissions.Invoices.View];
}

public sealed class GetPurchaseOrderProgressQueryHandler(IPurchaseOrderProgressQueryService service)
    : IRequestHandler<GetPurchaseOrderProgressQuery, PurchaseOrderProgressDto>
{
    public Task<PurchaseOrderProgressDto> Handle(GetPurchaseOrderProgressQuery query, CancellationToken ct)
        => service.GetAsync(query.OrderId, ct);
}
