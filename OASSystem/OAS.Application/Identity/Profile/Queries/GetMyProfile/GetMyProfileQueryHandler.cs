using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Identity.Profile;

namespace OAS.Application.Identity.Profile.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler(ICurrentUser currentUser, IIdentityRepository repository)
    : IRequestHandler<GetMyProfileQuery, MyProfileDto>
{
    public async Task<MyProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var record = await repository.GetUserAsync(userId, false, cancellationToken) ?? throw new ForbiddenException();
        if (!record.User.IsActive) throw new ForbiddenException();
        return Map(record);
    }

    internal static MyProfileDto Map(IdentityUserRecord record) => new(
        record.User.Id, record.User.UserName, record.User.FirstName, record.User.LastName,
        record.User.DisplayName, record.User.Email, record.User.PhoneNumber,
        record.Roles.Select(x => x.Name).OrderBy(x => x).ToArray(),
        Convert.ToBase64String(record.User.RowVersion));
}
