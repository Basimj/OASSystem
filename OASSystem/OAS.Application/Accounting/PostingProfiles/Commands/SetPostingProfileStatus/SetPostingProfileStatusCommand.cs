using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.PostingProfiles;

namespace OAS.Application.Accounting.PostingProfiles.Commands.SetPostingProfileStatus;

public sealed record SetPostingProfileStatusCommand(Guid Id, SetPostingProfileStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.PostingProfiles.Disable];
}
