using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Categories;

public sealed class ProductCategorySpecificationFactory : ICrudSpecificationFactory<ProductCategory>
{
    public ISpecification<ProductCategory> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<ProductCategory>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.Code.Contains(search) ||
                x.NameAr.Contains(search) ||
                (x.NameEn != null && x.NameEn.Contains(search)));
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
            return nameof(ProductCategory.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "code" => nameof(ProductCategory.Code),
            "name" => nameof(ProductCategory.NameAr),
            "namear" => nameof(ProductCategory.NameAr),
            "nameen" => nameof(ProductCategory.NameEn),
            "isactive" => nameof(ProductCategory.IsActive),
            _ => nameof(ProductCategory.Code)
        };
    }
}
