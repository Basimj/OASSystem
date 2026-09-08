using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Identity.Authentication;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Authentication.Commands;

public sealed class CompletePasswordSetupCommandHandler(
    ICurrentUser currentUser,
    IIdentityRepository repository,
    IPasswordService passwordService,
    TimeProvider timeProvider) : IRequestHandler<CompletePasswordSetupCommand, CurrentUserDto>
{
    private const int PasswordHistoryDepth = 5;

    public async Task<CurrentUserDto> Handle(CompletePasswordSetupCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var record = await repository.GetUserAsync(userId, true, cancellationToken)
            ?? throw new ForbiddenException();
        if (!record.User.IsActive)
            throw new ForbiddenException();
        if (!record.User.MustChangePassword)
            throw new ForbiddenException("Password setup is not required for this account.", "identity_password_setup_not_required");

        var history = await repository.GetPasswordHistoryAsync(userId, PasswordHistoryDepth, cancellationToken);
        var reusesCurrentCredential = !record.User.CanUsePasswordlessBootstrap() &&
            passwordService.Verify(record.User, request.Request.NewPassword) != PasswordCheckResult.Failed;
        var reusesRecentPermanentPassword = history.Any(x =>
            passwordService.VerifyHash(record.User, x.PasswordHash, request.Request.NewPassword) != PasswordCheckResult.Failed);

        if (reusesCurrentCredential || reusesRecentPermanentPassword)
            throw new ConflictException("identity_password_reused", "The password was used recently.");

        record.User.SetPasswordHash(passwordService.Hash(record.User, request.Request.NewPassword));
        record.User.CompleteRequiredPasswordChange();
        record.User.RecordSuccessfulLogin(timeProvider.GetUtcNow());
        await repository.AddPasswordHistoryAsync(
            UserPasswordHistory.Create(Guid.NewGuid(), userId, record.User.PasswordHash, timeProvider.GetUtcNow()),
            cancellationToken);

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
