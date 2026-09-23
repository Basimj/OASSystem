using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Products;

public sealed class ProductSpecificationFactory : ICrudSpecificationFactory<Product>
{
    public ISpecification<Product> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<Product>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.ProductCode.Contains(search) ||
                x.NameAr.Contains(search) ||
                (x.NameEn != null && x.NameEn.Contains(search)) ||
                (x.Description != null && x.Description.Contains(search)));
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
            return nameof(Product.ProductCode);

        return requested.Trim().ToLowerInvariant() switch
        {
            "productcode" => nameof(Product.ProductCode),
            "code" => nameof(Product.ProductCode),
            "name" => nameof(Product.NameAr),
            "namear" => nameof(Product.NameAr),
            "nameen" => nameof(Product.NameEn),
            "producttypeid" => nameof(Product.ProductTypeId),
            "isstockitem" => nameof(Product.IsStockItem),
            "isactive" => nameof(Product.IsActive),
            _ => nameof(Product.ProductCode)
        };
    }
}
