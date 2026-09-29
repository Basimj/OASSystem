using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Mapping;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseReceipts;

namespace OAS.Application.Purchasing.PurchaseReceipts.Queries;

public sealed record GetPurchaseReceiptByIdQuery(Guid Id):IQuery<PurchaseReceiptDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[PurchasingPermissions.Receipts.View];}
public sealed record GetPurchaseReceiptsQuery(PageRequest Request,PurchaseReceiptStatus? Status=null,Guid? PurchaseOrderId=null,Guid? SupplierId=null):IQuery<PagedResult<PurchaseReceiptDto>>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[PurchasingPermissions.Receipts.View];}
public sealed class GetPurchaseReceiptByIdQueryHandler(IPurchaseReceiptRepository repository,PurchasingMapper mapper):IRequestHandler<GetPurchaseReceiptByIdQuery,PurchaseReceiptDto>{public async Task<PurchaseReceiptDto> Handle(GetPurchaseReceiptByIdQuery q,CancellationToken ct)=>await mapper.ToDtoAsync(await repository.GetByIdAsync(q.Id,ct)??throw new NotFoundException("PurchaseReceipt",q.Id),ct);}
public sealed class GetPurchaseReceiptsQueryHandler(IPurchaseReceiptRepository repository,PurchasingMapper mapper):IRequestHandler<GetPurchaseReceiptsQuery,PagedResult<PurchaseReceiptDto>>{public async Task<PagedResult<PurchaseReceiptDto>> Handle(GetPurchaseReceiptsQuery q,CancellationToken ct){var r=q.Request.Normalize();var p=await repository.GetPageAsync(r,q.Status,q.PurchaseOrderId,q.SupplierId,ct);var items=new List<PurchaseReceiptDto>(p.Items.Count);foreach(var e in p.Items)items.Add(await mapper.ToDtoAsync(e,ct));return new(){Items=items,PageNumber=r.PageNumber,PageSize=r.PageSize,TotalCount=p.TotalCount};}}
