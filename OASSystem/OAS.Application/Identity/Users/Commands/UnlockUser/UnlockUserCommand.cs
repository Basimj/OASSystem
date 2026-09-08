using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;
using OAS.Contracts.Identity.Users;

namespace OAS.Application.Identity.Users.Commands.UnlockUser;

public sealed record UnlockUserCommand(Guid UserId, UnlockUserRequest Request) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [IdentityPermissions.UsersUnlock];
}
