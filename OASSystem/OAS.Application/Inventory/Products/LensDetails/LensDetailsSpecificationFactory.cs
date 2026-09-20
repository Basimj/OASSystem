using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using DomainLensDetails = OAS.Domain.Entities.Inventory.LensDetails;

namespace OAS.Application.Inventory.Products.LensDetails;

public sealed class LensDetailsSpecificationFactory : ICrudSpecificationFactory<DomainLensDetails>
{
    public ISpecification<DomainLensDetails> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<DomainLensDetails>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.LensType.Contains(search) ||
                (x.Material != null && x.Material.Contains(search)) ||
                (x.Coating != null && x.Coating.Contains(search)));
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
            return nameof(DomainLensDetails.LensType);

        return requested.Trim().ToLowerInvariant() switch
        {
            "lenstype" => nameof(DomainLensDetails.LensType),
            "material" => nameof(DomainLensDetails.Material),
            "coating" => nameof(DomainLensDetails.Coating),
            _ => nameof(DomainLensDetails.LensType)
        };
    }
}
