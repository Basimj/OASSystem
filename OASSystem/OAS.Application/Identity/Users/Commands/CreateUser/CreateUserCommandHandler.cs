using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;
using OAS.Domain.Identity.Constants;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    IIdentityRepository repository,
    IPasswordService passwordService,
    ITemporaryPasswordGenerator temporaryPasswordGenerator,
    ICurrentUser currentUser) : IRequestHandler<CreateUserCommand, CreateUserOutcome>
{
    public async Task<CreateUserOutcome> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;
        var normalizedUserName = UserAccount.Normalize(dto.UserName);
        if (await repository.UserNameExistsAsync(normalizedUserName, null, cancellationToken))
            throw new ConflictException("identity_username_exists", "User name already exists.");

        var normalizedEmail = string.IsNullOrWhiteSpace(dto.Email) ? null : UserAccount.Normalize(dto.Email);
        if (normalizedEmail is not null && await repository.EmailExistsAsync(normalizedEmail, null, cancellationToken))
            throw new ConflictException("identity_email_exists", "Email already exists.");

        var roleIds = dto.RoleIds.Distinct().ToArray();
        var roles = await repository.GetRolesByIdsAsync(roleIds, cancellationToken);
        if (roles.Count != roleIds.Length)
            throw new ConflictException("identity_role_not_found", "One or more roles do not exist.");

        if (roles.Any(x => string.Equals(x.Name, IdentityRoleNames.Administrator, StringComparison.OrdinalIgnoreCase)))
        {
            var actor = await UserManagementGuard.GetActorAsync(currentUser, repository, cancellationToken);
            if (!actor.User.IsSuperAdmin)
                throw new ForbiddenException("Super Administrator privileges are required.", "identity_super_admin_required");
        }

        var user = UserAccount.Create(Guid.NewGuid(), dto.UserName, dto.FirstName, dto.LastName, dto.Email, dto.IsActive, false, true);
        var temporaryPassword = temporaryPasswordGenerator.Generate();
        user.SetPasswordHash(passwordService.Hash(user, temporaryPassword));
        user.RequirePasswordChange();

        await repository.AddUserAsync(user, cancellationToken);
        foreach (var role in roles)
            await repository.AddUserRoleAsync(UserRole.Create(Guid.NewGuid(), user.Id, role.Id), cancellationToken);


        return new CreateUserOutcome(user.Id, temporaryPassword);
    }
}
