using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Queries.GetPostingProfileById;

public sealed record GetPostingProfileByIdQuery(Guid Id)
    : GetEntityByIdQuery<PostingProfile, Guid, PostingProfileDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PostingProfiles.View];
}
