using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.PostingProfiles.Mapping;
using OAS.Application.Accounting.PostingProfiles.Specifications;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Queries.GetPostingProfiles;

public sealed class GetPostingProfilesQueryHandler(
    IReadRepository<PostingProfile, Guid> repository,
    PostingProfileMapper mapper)
    : IRequestHandler<GetPostingProfilesQuery, PagedResult<PostingProfileDto>>
{
    private static readonly PostingProfilePageSpecification SpecificationFactory = new();

    public async Task<PagedResult<PostingProfileDto>> Handle(
        GetPostingProfilesQuery request,
        CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = SpecificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);

        return new PagedResult<PostingProfileDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
