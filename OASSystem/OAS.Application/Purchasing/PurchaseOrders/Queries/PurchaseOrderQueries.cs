using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Mapping;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseOrders;

namespace OAS.Application.Purchasing.PurchaseOrders.Queries;

public sealed record GetPurchaseOrderByIdQuery(Guid Id) : IQuery<PurchaseOrderDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Orders.View]; }
public sealed record GetPurchaseOrdersQuery(PageRequest Request, PurchaseOrderStatus? Status=null, Guid? SupplierId=null, Guid? WarehouseId=null)
    : IQuery<PagedResult<PurchaseOrderDto>>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Orders.View]; }

public sealed class GetPurchaseOrderByIdQueryHandler(IPurchaseOrderRepository repository, PurchasingMapper mapper) : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto>
{ public async Task<PurchaseOrderDto> Handle(GetPurchaseOrderByIdQuery q,CancellationToken ct)=>await mapper.ToDtoAsync(await repository.GetByIdAsync(q.Id,ct)??throw new NotFoundException("PurchaseOrder",q.Id),ct); }

public sealed class GetPurchaseOrdersQueryHandler(IPurchaseOrderRepository repository, PurchasingMapper mapper) : IRequestHandler<GetPurchaseOrdersQuery,PagedResult<PurchaseOrderDto>>
{
    public async Task<PagedResult<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery q,CancellationToken ct){var r=q.Request.Normalize();var p=await repository.GetPageAsync(r,q.Status,q.SupplierId,q.WarehouseId,ct);var items=new List<PurchaseOrderDto>(p.Items.Count);foreach(var e in p.Items)items.Add(await mapper.ToDtoAsync(e,ct));return new(){Items=items,PageNumber=r.PageNumber,PageSize=r.PageSize,TotalCount=p.TotalCount};}
}
