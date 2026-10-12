using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Entities.Inventory;

namespace OAS.Application.Inventory.Products.Units;

public sealed class UnitSpecificationFactory : ICrudSpecificationFactory<Unit>
{
    public ISpecification<Unit> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<Unit>();

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
            return nameof(Unit.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "code" => nameof(Unit.Code),
            "name" => nameof(Unit.Name),
            "namear" => nameof(Unit.Name),
            "isactive" => nameof(Unit.IsActive),
            _ => nameof(Unit.Code)
        };
    }
}
