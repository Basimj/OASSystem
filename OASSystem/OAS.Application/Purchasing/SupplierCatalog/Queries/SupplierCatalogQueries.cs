using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Mapping;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.SupplierCatalog;

namespace OAS.Application.Purchasing.SupplierCatalog.Queries;

public sealed record GetSupplierCatalogItemByIdQuery(Guid Id) : IQuery<SupplierCatalogItemDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Catalog.View];
}

public sealed record GetSupplierCatalogQuery(PageRequest Request, Guid? SupplierId = null, Guid? ProductVariantId = null, bool? IsActive = null)
    : IQuery<PagedResult<SupplierCatalogItemDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Catalog.View];
}

public sealed class GetSupplierCatalogItemByIdQueryHandler(ISupplierCatalogRepository repository, PurchasingMapper mapper)
    : IRequestHandler<GetSupplierCatalogItemByIdQuery, SupplierCatalogItemDto>
{
    public async Task<SupplierCatalogItemDto> Handle(GetSupplierCatalogItemByIdQuery query, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(query.Id, cancellationToken) ?? throw new NotFoundException("SupplierCatalogItem", query.Id);
        return await mapper.ToDtoAsync(item, cancellationToken);
    }
}

public sealed class GetSupplierCatalogQueryHandler(ISupplierCatalogRepository repository, PurchasingMapper mapper)
    : IRequestHandler<GetSupplierCatalogQuery, PagedResult<SupplierCatalogItemDto>>
{
    public async Task<PagedResult<SupplierCatalogItemDto>> Handle(GetSupplierCatalogQuery query, CancellationToken cancellationToken)
    {
        var request = query.Request.Normalize();
        var page = await repository.GetPageAsync(request, query.SupplierId, query.ProductVariantId, query.IsActive, cancellationToken);
        var items = new List<SupplierCatalogItemDto>(page.Items.Count);
        foreach (var item in page.Items) items.Add(await mapper.ToDtoAsync(item, cancellationToken));
        return new PagedResult<SupplierCatalogItemDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = page.TotalCount };
    }
}
