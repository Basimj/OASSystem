using System.Linq.Expressions;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Specifications;

public sealed class PostingProfilePageSpecification
    : ICrudSpecificationFactory<PostingProfile>
{
    public ISpecification<PostingProfile> CreatePageSpecification(PageRequest request)
    {
        var normalized = request.Normalize();
        var specification = new Specification<PostingProfile>();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search.Trim();
            specification.Where(x =>
                x.Code.Contains(search) ||
                x.Name.Contains(search) ||
                x.Module.Contains(search) ||
                x.DocumentType.Contains(search));
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
            return nameof(PostingProfile.Code);

        return requested.Trim().ToLowerInvariant() switch
        {
            "name" => nameof(PostingProfile.Name),
            "module" => nameof(PostingProfile.Module),
            "documenttype" => nameof(PostingProfile.DocumentType),
            "isactive" => nameof(PostingProfile.IsActive),
            "code" => nameof(PostingProfile.Code),
            _ => nameof(PostingProfile.Code)
        };
    }
}
