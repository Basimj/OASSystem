using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Identity.Authentication;

namespace OAS.Application.Identity.Authentication.Queries;

public sealed class GetCurrentUserQueryHandler(ICurrentUser currentUser, IIdentityRepository repository)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var record = await repository.GetUserAsync(userId, false, cancellationToken)
            ?? throw new ForbiddenException();
        if (!record.User.IsActive) throw new ForbiddenException();

        return new CurrentUserDto(
            record.User.Id,
            record.User.UserName,
            record.User.DisplayName,
            record.User.Email,
            record.Roles.Select(x => x.Name).OrderBy(x => x).ToArray(),
            record.User.IsSuperAdmin,
            record.User.MustChangePassword);
    }
}
