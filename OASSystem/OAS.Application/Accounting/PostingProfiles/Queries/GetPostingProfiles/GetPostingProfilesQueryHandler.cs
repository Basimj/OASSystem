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
    IReadRepository<PostingProfileLine, Guid> lineRepository,
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

        // Load line metadata for the visible page in one batch so list counts are real.
        var ids = page.Items.Select(x => x.Id).ToArray();
        IReadOnlyList<PostingProfileLine> lines = ids.Length == 0
            ? Array.Empty<PostingProfileLine>()
            : await lineRepository.ListAsync(new OAS.Application.Abstractions.Persistence.Specifications.Specification<PostingProfileLine>()
                .Where(x => ids.Contains(x.PostingProfileId)), cancellationToken);
        var byProfile = lines.GroupBy(x => x.PostingProfileId).ToDictionary(x => x.Key, x => x.AsEnumerable());

        return new PagedResult<PostingProfileDto>
        {
            Items = page.Items.Select(x => mapper.ToRead(x, byProfile.GetValueOrDefault(x.Id) ?? Array.Empty<PostingProfileLine>())).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
