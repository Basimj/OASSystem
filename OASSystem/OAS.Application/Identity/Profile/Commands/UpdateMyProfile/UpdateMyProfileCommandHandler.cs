using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;

namespace OAS.Application.Identity.Profile.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandHandler(ICurrentUser currentUser, IIdentityRepository repository)
    : IRequestHandler<UpdateMyProfileCommand>
{
    public async Task Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var record = await repository.GetUserAsync(userId, true, cancellationToken)
            ?? throw new ForbiddenException();

        RowVersionCodec.EnsureMatches(record.User.RowVersion, request.Request.RowVersion);
        record.User.UpdatePersonalName(request.Request.FirstName, request.Request.LastName);
    }
}
