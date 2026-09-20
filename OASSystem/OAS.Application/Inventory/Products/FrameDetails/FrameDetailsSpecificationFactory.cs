using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using DomainFrameDetails = OAS.Domain.Entities.Inventory.FrameDetails;

namespace OAS.Application.Inventory.Products.FrameDetails;

public sealed class FrameDetailsSpecificationFactory : ICrudSpecificationFactory<DomainFrameDetails>
{
    public ISpecification<DomainFrameDetails> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<DomainFrameDetails>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.Model.Contains(search) ||
                (x.Material != null && x.Material.Contains(search)) ||
                (x.Shape != null && x.Shape.Contains(search)));
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
            return nameof(DomainFrameDetails.Model);

        return requested.Trim().ToLowerInvariant() switch
        {
            "model" => nameof(DomainFrameDetails.Model),
            "material" => nameof(DomainFrameDetails.Material),
            "shape" => nameof(DomainFrameDetails.Shape),
            _ => nameof(DomainFrameDetails.Model)
        };
    }
}
