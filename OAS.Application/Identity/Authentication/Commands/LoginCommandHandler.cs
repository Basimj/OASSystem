using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Authentication.Models;
using OAS.Contracts.Identity.Authentication;

namespace OAS.Application.Identity.Authentication.Commands;

public sealed class LoginCommandHandler(
    IIdentityRepository repository,
    IPasswordService passwordService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<LoginCommand> validator) : IRequestHandler<LoginCommand, LoginResult>
{
    private const int MaximumFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => x.ErrorCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray());
            return LoginResult.Validation(errors);
        }

        var record = await repository.FindByLoginAsync(request.Request.Login, true, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (record is null || !record.User.IsActive || record.User.IsLockedOut(now))
        {
            passwordService.SimulateVerification(request.Request.Password);
            return LoginResult.InvalidCredentials();
        }

        var passwordResult = passwordService.Verify(record.User, request.Request.Password);
        if (passwordResult == PasswordCheckResult.Failed)
        {
            record.User.RecordFailedAccess(now, MaximumFailedAttempts, LockoutDuration);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return LoginResult.InvalidCredentials();
        }

        if (passwordResult == PasswordCheckResult.SuccessRehashNeeded)
            record.User.SetPasswordHash(passwordService.Hash(record.User, request.Request.Password));

        record.User.RecordSuccessfulLogin(now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return LoginResult.Success(ToDto(record));
    }

    private static CurrentUserDto ToDto(IdentityUserRecord record) => new(
        record.User.Id,
        record.User.UserName,
        record.User.DisplayName,
        record.User.Email,
        record.Roles.Select(x => x.Name).OrderBy(x => x).ToArray(),
        record.User.IsSuperAdmin,
        record.User.MustChangePassword);
}
