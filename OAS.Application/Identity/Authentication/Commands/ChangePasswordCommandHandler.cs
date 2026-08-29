using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Authentication.Models;
using OAS.Contracts.Identity.Authentication;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Authentication.Commands;

public sealed class ChangePasswordCommandHandler(
    ICurrentUser currentUser,
    IIdentityRepository repository,
    IPasswordService passwordService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<ChangePasswordCommand> validator) : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
{
    private const int PasswordHistoryDepth = 5;

    public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => x.ErrorCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray());
            return ChangePasswordResult.Validation(errors);
        }

        if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var userId))
            return ChangePasswordResult.Failure(ChangePasswordFailureKind.Forbidden, "forbidden");

        var record = await repository.GetUserAsync(userId, true, cancellationToken);
        if (record is null)
            return ChangePasswordResult.Failure(ChangePasswordFailureKind.Forbidden, "forbidden");

        if (passwordService.Verify(record.User, request.Request.CurrentPassword) == PasswordCheckResult.Failed)
            return ChangePasswordResult.Failure(ChangePasswordFailureKind.CurrentPasswordInvalid, "identity_current_password_invalid");

        var history = await repository.GetPasswordHistoryAsync(userId, PasswordHistoryDepth, cancellationToken);
        if (history.Any(x => passwordService.VerifyHash(record.User, x.PasswordHash, request.Request.NewPassword) != PasswordCheckResult.Failed))
            return ChangePasswordResult.Failure(ChangePasswordFailureKind.PasswordReused, "identity_password_reused");

        record.User.SetPasswordHash(passwordService.Hash(record.User, request.Request.NewPassword));
        record.User.CompleteRequiredPasswordChange();
        await repository.AddPasswordHistoryAsync(
            UserPasswordHistory.Create(Guid.NewGuid(), userId, record.User.PasswordHash, timeProvider.GetUtcNow()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = new CurrentUserDto(
            record.User.Id, record.User.UserName, record.User.DisplayName, record.User.Email,
            record.Roles.Select(x => x.Name).OrderBy(x => x).ToArray(),
            record.User.IsSuperAdmin, record.User.MustChangePassword);
        return ChangePasswordResult.Success(user);
    }
}
