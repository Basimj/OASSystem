using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Identity.Authentication;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Profile.Commands.ChangeMyPassword;

public sealed class ChangeMyPasswordCommandHandler(
    ICurrentUser currentUser, IIdentityRepository repository, IPasswordService passwordService, TimeProvider timeProvider)
    : IRequestHandler<ChangeMyPasswordCommand, CurrentUserDto>
{
    private const int PasswordHistoryDepth = 5;

    public async Task<CurrentUserDto> Handle(ChangeMyPasswordCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId)) throw new ForbiddenException();
        var record = await repository.GetUserAsync(userId, true, cancellationToken) ?? throw new ForbiddenException();
        if (!record.User.IsActive || record.User.MustChangePassword) throw new ForbiddenException();

        if (passwordService.Verify(record.User, request.Request.CurrentPassword) == PasswordCheckResult.Failed)
            throw new ForbiddenException("Current password is incorrect.", "identity_current_password_invalid");

        var history = await repository.GetPasswordHistoryAsync(userId, PasswordHistoryDepth, cancellationToken);
        if (passwordService.Verify(record.User, request.Request.NewPassword) != PasswordCheckResult.Failed ||
            history.Any(x => passwordService.VerifyHash(record.User, x.PasswordHash, request.Request.NewPassword) != PasswordCheckResult.Failed))
            throw new ConflictException("identity_password_reused", "The password was used recently.");

        record.User.SetPasswordHash(passwordService.Hash(record.User, request.Request.NewPassword));
        await repository.AddPasswordHistoryAsync(
            UserPasswordHistory.Create(Guid.NewGuid(), userId, record.User.PasswordHash, timeProvider.GetUtcNow()), cancellationToken);

        return new CurrentUserDto(record.User.Id, record.User.UserName, record.User.DisplayName, record.User.Email,
            record.Roles.Select(x => x.Name).OrderBy(x => x).ToArray(), record.User.IsSuperAdmin, record.User.MustChangePassword);
    }
}
