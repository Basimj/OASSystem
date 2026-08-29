using FluentValidation;
using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Models;
using OAS.Contracts.Identity.Users;
using OAS.Domain.Identity.Constants;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Users.Commands;

public sealed class CreateUserCommandHandler(
    IIdentityRepository repository,
    IPasswordService passwordService,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IValidator<CreateUserCommand> validator) : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => x.ErrorCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray());
            return CreateUserResult.Validation(errors);
        }

        var dto = request.Request;
        var normalizedUserName = UserAccount.Normalize(dto.UserName);
        if (await repository.UserNameExistsAsync(normalizedUserName, cancellationToken))
            return CreateUserResult.Conflict("identity_username_exists");

        var normalizedEmail = string.IsNullOrWhiteSpace(dto.Email) ? null : UserAccount.Normalize(dto.Email);
        if (normalizedEmail is not null && await repository.EmailExistsAsync(normalizedEmail, cancellationToken))
            return CreateUserResult.Conflict("identity_email_exists");

        var role = await repository.GetRoleAsync(dto.RoleId, cancellationToken);
        if (role is null) return CreateUserResult.Conflict("identity_role_not_found");

        if (string.Equals(role.Name, IdentityRoleNames.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            if (!currentUser.IsAuthenticated || !Guid.TryParse(currentUser.UserId, out var actorId))
                return CreateUserResult.Forbidden();
            var actor = await repository.GetUserAsync(actorId, false, cancellationToken);
            if (actor is null || !actor.User.IsSuperAdmin)
                return CreateUserResult.Forbidden("identity_super_admin_required");
        }

        var user = UserAccount.Create(Guid.NewGuid(), dto.UserName, dto.FirstName, dto.LastName, dto.Email, dto.IsActive);
        user.SetPasswordHash(passwordService.Hash(user, dto.Password));
        await repository.AddUserAsync(user, cancellationToken);
        await repository.AddUserRoleAsync(UserRole.Create(Guid.NewGuid(), user.Id, role.Id), cancellationToken);
        await repository.AddPasswordHistoryAsync(UserPasswordHistory.Create(Guid.NewGuid(), user.Id, user.PasswordHash, timeProvider.GetUtcNow()), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateUserResult.Success(new UserDto(
            user.Id, user.UserName, user.FirstName, user.LastName, user.DisplayName, user.Email,
            user.IsActive, user.IsSuperAdmin, user.MustChangePassword, [role.Name], user.CreatedAtUtc, user.LastLoginAtUtc));
    }
}
