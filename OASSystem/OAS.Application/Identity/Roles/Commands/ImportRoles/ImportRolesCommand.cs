using OAS.Application.Abstractions.Messaging;
using OAS.Application.Identity.Authorization;
using OAS.Contracts.Identity.Roles;

namespace OAS.Application.Identity.Roles.Commands.ImportRoles;

public sealed record ImportRolesCommand(ImportRolesRequest Request) : ICommand<ImportRolesResultDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [IdentityPermissions.RolesManage];
}
