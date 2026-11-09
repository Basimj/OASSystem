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

        // The specification stores a single criteria expression; compose all filters
        // to preserve search, status, module and document filters across pagination.
        var search = normalized.Search;
        var module = normalized.PostingModule;
        var documentType = normalized.PostingDocumentType;
        var status = normalized.Status;
        specification.Where(x =>
            (search == null || x.Code.Contains(search) || x.Name.Contains(search) ||
             x.Module.Contains(search) || x.DocumentType.Contains(search)) &&
            (module == null || x.Module == module) &&
            (documentType == null || x.DocumentType == documentType) &&
            (status != "active" || x.IsActive) &&
            (status != "inactive" || !x.IsActive));

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
