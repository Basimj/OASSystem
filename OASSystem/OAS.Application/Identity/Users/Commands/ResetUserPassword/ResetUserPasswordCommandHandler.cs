using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Users.Commands.ResetUserPassword;

public sealed class ResetUserPasswordCommandHandler(
    IIdentityRepository repository,
    IPasswordService passwordService,
    ITemporaryPasswordGenerator temporaryPasswordGenerator,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<ResetUserPasswordCommand, string>
{
    private const int PasswordHistoryDepth = 5;

    public async Task<string> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var target = await repository.GetUserAsync(request.UserId, true, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);
        RowVersionCodec.EnsureMatches(target.User.RowVersion, request.Request.RowVersion);
        await UserManagementGuard.EnsureCanManageTargetAsync(target, currentUser, repository, cancellationToken);

        var history = await repository.GetPasswordHistoryAsync(target.User.Id, PasswordHistoryDepth, cancellationToken);
        var previousPasswordHash = target.User.PasswordHash;

        // Preserve the current permanent credential before replacing it. Temporary
        // credentials are deliberately excluded from permanent-password history.
        if (!target.User.MustChangePassword &&
            !string.IsNullOrWhiteSpace(previousPasswordHash) &&
            !history.Any(item => string.Equals(item.PasswordHash, previousPasswordHash, StringComparison.Ordinal)))
        {
            await repository.AddPasswordHistoryAsync(
                UserPasswordHistory.Create(Guid.NewGuid(), target.User.Id, previousPasswordHash, timeProvider.GetUtcNow()),
                cancellationToken);
        }

        var temporaryPassword = GenerateUnusedTemporaryPassword(target.User, history);
        target.User.SetPasswordHash(passwordService.Hash(target.User, temporaryPassword));
        target.User.RequirePasswordChange();
        target.User.ResetAccessFailures();

        return temporaryPassword;
    }

    private string GenerateUnusedTemporaryPassword(UserAccount user, IReadOnlyList<UserPasswordHistory> history)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = temporaryPasswordGenerator.Generate();
            var reused = history.Any(x => passwordService.VerifyHash(user, x.PasswordHash, candidate) != PasswordCheckResult.Failed);
            if (!reused && passwordService.Verify(user, candidate) == PasswordCheckResult.Failed) return candidate;
        }

        throw new InvalidOperationException("Unable to generate a unique temporary password.");
    }
}
