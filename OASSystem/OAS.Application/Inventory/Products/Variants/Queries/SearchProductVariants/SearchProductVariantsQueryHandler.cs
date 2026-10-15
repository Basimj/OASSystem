using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Inventory.Products;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Variants.Queries.SearchProductVariants;

public sealed class SearchProductVariantsQueryHandler(
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<Product, Guid> products)
    : IRequestHandler<SearchProductVariantsQuery, IReadOnlyList<ProductVariantLookupDto>>
{
    public async Task<IReadOnlyList<ProductVariantLookupDto>> Handle(
        SearchProductVariantsQuery request,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 50);
        var search = request.Search?.Trim();

        var matchingProductIds = new HashSet<Guid>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var matchingProducts = await products.ListAsync(
                new Specification<Product>()
                    .Where(x => x.IsActive &&
                        (x.Name.Contains(search) || x.ProductCode.Contains(search)))
                    .ApplyPaging(0, 100),
                cancellationToken);

            matchingProductIds.UnionWith(matchingProducts.Select(x => x.Id));
        }

        var variantSpec = new Specification<ProductVariant>();
        if (string.IsNullOrWhiteSpace(search))
        {
            variantSpec.Where(x => x.IsActive);
        }
        else
        {
            variantSpec.Where(x => x.IsActive &&
                (x.SKU.Contains(search) ||
                 (x.Barcode != null && x.Barcode.Contains(search)) ||
                 (x.VariantName != null && x.VariantName.Contains(search)) ||
                 matchingProductIds.Contains(x.ProductId)));
        }

        variantSpec
            .AddSort(nameof(ProductVariant.SKU), SortDirection.Ascending)
            .ApplyPaging(0, take);

        var variantRows = await variants.ListAsync(variantSpec, cancellationToken);
        if (variantRows.Count == 0)
            return [];

        var productIds = variantRows.Select(x => x.ProductId).Distinct().ToArray();
        var productRows = await products.ListAsync(
            new Specification<Product>().Where(x => x.IsActive && productIds.Contains(x.Id)),
            cancellationToken);
        var productMap = productRows.ToDictionary(x => x.Id);

        return variantRows
            .Where(x => productMap.ContainsKey(x.ProductId))
            .Select(x =>
            {
                var product = productMap[x.ProductId];
                return new ProductVariantLookupDto(
                    x.Id,
                    x.ProductId,
                    product.ProductCode,
                    product.Name,
                    x.SKU,
                    x.Barcode,
                    x.VariantName,
                    x.Color,
                    x.Size,
                    x.UnitId,
                    x.IsActive);
            })
            .ToArray();
    }
}
