using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Commands.UpdatePostingProfile;

public sealed record UpdatePostingProfileCommand(Guid Id, UpdatePostingProfileRequest Data)
    : UpdateEntityCommand<PostingProfile, Guid, UpdatePostingProfileRequest>(Id, Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PostingProfiles.Edit];
}
