using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Mapping;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseInvoices;

namespace OAS.Application.Purchasing.PurchaseInvoices.Queries;

public sealed record GetPurchaseInvoiceByIdQuery(Guid Id):IQuery<PurchaseInvoiceDto>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[PurchasingPermissions.Invoices.View];}
public sealed record GetPurchaseInvoicesQuery(PageRequest Request,PurchaseInvoiceStatus? Status=null,Guid? SupplierId=null):IQuery<PagedResult<PurchaseInvoiceDto>>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[PurchasingPermissions.Invoices.View];}
public sealed class GetPurchaseInvoiceByIdQueryHandler(IPurchaseInvoiceRepository repository,PurchasingMapper mapper):IRequestHandler<GetPurchaseInvoiceByIdQuery,PurchaseInvoiceDto>{public async Task<PurchaseInvoiceDto> Handle(GetPurchaseInvoiceByIdQuery q,CancellationToken ct)=>await mapper.ToDtoAsync(await repository.GetByIdAsync(q.Id,ct)??throw new NotFoundException("PurchaseInvoice",q.Id),ct);}
public sealed class GetPurchaseInvoicesQueryHandler(IPurchaseInvoiceRepository repository,PurchasingMapper mapper):IRequestHandler<GetPurchaseInvoicesQuery,PagedResult<PurchaseInvoiceDto>>{public async Task<PagedResult<PurchaseInvoiceDto>> Handle(GetPurchaseInvoicesQuery q,CancellationToken ct){var r=q.Request.Normalize();var p=await repository.GetPageAsync(r,q.Status,q.SupplierId,ct);var items=new List<PurchaseInvoiceDto>(p.Items.Count);foreach(var e in p.Items)items.Add(await mapper.ToDtoAsync(e,ct));return new(){Items=items,PageNumber=r.PageNumber,PageSize=r.PageSize,TotalCount=p.TotalCount};}}
