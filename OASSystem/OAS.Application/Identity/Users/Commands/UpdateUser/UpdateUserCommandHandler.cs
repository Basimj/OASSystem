using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler(IIdentityRepository repository, ICurrentUser currentUser)
    : IRequestHandler<UpdateUserCommand, Guid>
{
    public async Task<Guid> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var record = await repository.GetUserAsync(request.UserId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);
        RowVersionCodec.EnsureMatches(record.User.RowVersion, request.Request.RowVersion);
        await UserManagementGuard.EnsureCanManageTargetAsync(record, currentUser, repository, cancellationToken);

        var normalizedUserName = UserAccount.Normalize(request.Request.UserName);
        if (await repository.UserNameExistsAsync(normalizedUserName, request.UserId, cancellationToken))
            throw new ConflictException("identity_username_exists", "User name already exists.");

        var normalizedEmail = string.IsNullOrWhiteSpace(request.Request.Email) ? null : UserAccount.Normalize(request.Request.Email);
        if (normalizedEmail is not null && await repository.EmailExistsAsync(normalizedEmail, request.UserId, cancellationToken))
            throw new ConflictException("identity_email_exists", "Email already exists.");

        record.User.SetIdentity(request.Request.UserName, request.Request.FirstName, request.Request.LastName, request.Request.Email, request.Request.PhoneNumber);
        return record.User.Id;
    }
}
