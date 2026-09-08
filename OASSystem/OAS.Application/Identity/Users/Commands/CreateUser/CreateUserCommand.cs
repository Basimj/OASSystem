using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;
using OAS.Contracts.Identity.Users;

namespace OAS.Application.Identity.Users.Commands.CreateUser;

public sealed record CreateUserCommand(CreateUserRequest Request)
    : ICommand<CreateUserOutcome>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [IdentityPermissions.UsersCreate];
}

public sealed record CreateUserOutcome(Guid UserId, string TemporaryPassword);
