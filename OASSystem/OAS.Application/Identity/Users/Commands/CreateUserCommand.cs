using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;
using OAS.Application.Identity.Users.Models;
using OAS.Contracts.Identity.Users;

namespace OAS.Application.Identity.Users.Commands;

public sealed record CreateUserCommand(CreateUserRequest Request)
    : ICommand<CreateUserResult>, IAuthorizedRequest, IManualValidationRequest, IExpectedFailureRequest<CreateUserResult>
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [IdentityPermissions.UsersManage];
    public CreateUserResult Forbidden(string errorCode) => CreateUserResult.Forbidden(errorCode);
}
