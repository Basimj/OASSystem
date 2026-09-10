using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandHandler(IIdentityRepository repository)
    : IRequestHandler<CreateRoleCommand, Guid>
{
    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var name = request.Request.Name.Trim();
        var normalizedName = UserAccount.Normalize(name);
        if (await repository.RoleExistsAsync(normalizedName, cancellationToken))
            throw new ConflictException("identity_role_exists", "Role already exists.");

        var role = Role.Create(Guid.NewGuid(), name, request.Request.DisplayName, isSystem: false);
        await repository.AddRoleAsync(role, cancellationToken);
        return role.Id;
    }
}
