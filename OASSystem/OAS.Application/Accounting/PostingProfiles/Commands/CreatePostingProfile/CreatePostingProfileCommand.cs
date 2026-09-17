using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Commands.CreatePostingProfile;

public sealed record CreatePostingProfileCommand(CreatePostingProfileRequest Data)
    : CreateEntityCommand<PostingProfile, Guid, CreatePostingProfileRequest>(Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PostingProfiles.Create];
}
