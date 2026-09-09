using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;

namespace OAS.Application.Identity.Users.Commands.SetUserProfileImage;

public sealed record SetUserProfileImageCommand(Guid UserId, string ContentType, byte[] Content) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => [IdentityPermissions.UsersEdit];
}
