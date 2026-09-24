using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.ProductTypes;

public sealed class ProductTypeSpecificationFactory : ICrudSpecificationFactory<ProductType>
{
    public ISpecification<ProductType> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<ProductType>();

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
            return nameof(ProductType.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "code" => nameof(ProductType.Code),
            "name" => nameof(ProductType.NameAr),
            "namear" => nameof(ProductType.NameAr),
            "nameen" => nameof(ProductType.NameEn),
            "isactive" => nameof(ProductType.IsActive),
            _ => nameof(ProductType.Code)
        };
    }
}
