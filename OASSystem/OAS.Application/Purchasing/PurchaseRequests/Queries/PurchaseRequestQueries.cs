using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Mapping;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseRequests;

namespace OAS.Application.Purchasing.PurchaseRequests.Queries;

public sealed record GetPurchaseRequestByIdQuery(Guid Id) : IQuery<PurchaseRequestDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Requests.View]; }
public sealed record GetPurchaseRequestsQuery(PageRequest Request, PurchaseRequestStatus? Status = null, Guid? WarehouseId = null)
    : IQuery<PagedResult<PurchaseRequestDto>>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Requests.View]; }

public sealed class GetPurchaseRequestByIdQueryHandler(IPurchaseRequestRepository repository, PurchasingMapper mapper)
    : IRequestHandler<GetPurchaseRequestByIdQuery, PurchaseRequestDto>
{
    public async Task<PurchaseRequestDto> Handle(GetPurchaseRequestByIdQuery query, CancellationToken ct) =>
        await mapper.ToDtoAsync(await repository.GetByIdAsync(query.Id, ct) ?? throw new NotFoundException("PurchaseRequest", query.Id), ct);
}

public sealed class GetPurchaseRequestsQueryHandler(IPurchaseRequestRepository repository, PurchasingMapper mapper)
    : IRequestHandler<GetPurchaseRequestsQuery, PagedResult<PurchaseRequestDto>>
{
    public async Task<PagedResult<PurchaseRequestDto>> Handle(GetPurchaseRequestsQuery query, CancellationToken ct)
    {
        var r = query.Request.Normalize();
        var page = await repository.GetPageAsync(r, query.Status, query.WarehouseId, ct);
        var items = new List<PurchaseRequestDto>(page.Items.Count);
        foreach (var item in page.Items) items.Add(await mapper.ToDtoAsync(item, ct));
        return new PagedResult<PurchaseRequestDto> { Items = items, PageNumber = r.PageNumber, PageSize = r.PageSize, TotalCount = page.TotalCount };
    }
}
