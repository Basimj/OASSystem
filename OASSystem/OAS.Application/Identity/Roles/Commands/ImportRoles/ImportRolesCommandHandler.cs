using MediatR;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Identity.Roles;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Roles.Commands.ImportRoles;

public sealed class ImportRolesCommandHandler(IIdentityRepository repository)
    : IRequestHandler<ImportRolesCommand, ImportRolesResultDto>
{
    public async Task<ImportRolesResultDto> Handle(ImportRolesCommand request, CancellationToken cancellationToken)
    {
        var existingRoles = await repository.ListRolesAsync(cancellationToken);
        var byName = existingRoles.ToDictionary(x => x.NormalizedName, StringComparer.Ordinal);
        var created = 0;
        var updated = 0;
        var unchanged = 0;

        foreach (var item in request.Request.Roles)
        {
            var name = item.Name.Trim();
            var displayName = item.DisplayName.Trim();
            var normalizedName = UserAccount.Normalize(name);

            if (byName.TryGetValue(normalizedName, out var existing))
            {
                if (string.Equals(existing.DisplayName, displayName, StringComparison.Ordinal))
                {
                    unchanged++;
                    continue;
                }

                existing.UpdateDisplayName(displayName);
                repository.UpdateRole(existing);
                updated++;
                continue;
            }

            var role = Role.Create(Guid.NewGuid(), name, displayName, isSystem: false);
            await repository.AddRoleAsync(role, cancellationToken);
            byName.Add(role.NormalizedName, role);
            created++;
        }

        return new ImportRolesResultDto(created, updated, unchanged);
    }
}
