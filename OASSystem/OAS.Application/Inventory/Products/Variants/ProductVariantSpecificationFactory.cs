using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Variants;

public sealed class ProductVariantSpecificationFactory : ICrudSpecificationFactory<ProductVariant>
{
    public ISpecification<ProductVariant> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<ProductVariant>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.SKU.Contains(search) ||
                (x.Barcode != null && x.Barcode.Contains(search)) ||
                (x.VariantName != null && x.VariantName.Contains(search)) ||
                (x.Color != null && x.Color.Contains(search)) ||
                (x.Size != null && x.Size.Contains(search)));
        }

        var sortBy = ResolveSortProperty(normalized.SortBy);
        specification.AddSort(sortBy, normalized.SortDirection);

        specification.ApplyPaging(
            (normalized.PageNumber - 1) * normalized.PageSize,
            normalized.PageSize);

        return specification;
    }

    private static string ResolveSortProperty(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
            return nameof(ProductVariant.SKU);

        return requested.Trim().ToLowerInvariant() switch
        {
            "sku" => nameof(ProductVariant.SKU),
            "barcode" => nameof(ProductVariant.Barcode),
            "name" => nameof(ProductVariant.VariantName),
            "variantname" => nameof(ProductVariant.VariantName),
            "color" => nameof(ProductVariant.Color),
            "size" => nameof(ProductVariant.Size),
            "purchaseprice" => nameof(ProductVariant.PurchasePrice),
            "sellingprice" => nameof(ProductVariant.SellingPrice),
            "isactive" => nameof(ProductVariant.IsActive),
            _ => nameof(ProductVariant.SKU)
        };
    }
}
