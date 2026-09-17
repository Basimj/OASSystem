using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Queries.GetPostingProfiles;

public sealed record GetPostingProfilesQuery(PageRequest Request)
    : GetEntityPageQuery<PostingProfile, Guid, PostingProfileDto>(Request),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PostingProfiles.View];
}
