using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Brands;

public sealed class BrandSpecificationFactory : ICrudSpecificationFactory<Brand>
{
    public ISpecification<Brand> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<Brand>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.Code.Contains(search) ||
                x.Name.Contains(search));
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
            return nameof(Brand.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "code" => nameof(Brand.Code),
            "name" => nameof(Brand.Name),
            "isactive" => nameof(Brand.IsActive),
            _ => nameof(Brand.Code)
        };
    }
}
