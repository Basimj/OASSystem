using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;
using OAS.Contracts.Identity.Users;

namespace OAS.Application.Identity.Users.Commands.ResetUserPassword;

public sealed record ResetUserPasswordCommand(Guid UserId, ResetUserPasswordRequest Request) : ICommand<string>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [IdentityPermissions.UsersResetPassword];
}
