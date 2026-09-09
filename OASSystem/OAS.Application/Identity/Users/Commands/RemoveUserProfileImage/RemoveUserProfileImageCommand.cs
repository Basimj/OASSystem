using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;

namespace OAS.Application.Identity.Users.Commands.RemoveUserProfileImage;

public sealed record RemoveUserProfileImageCommand(Guid UserId) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [IdentityPermissions.UsersEdit];
}
