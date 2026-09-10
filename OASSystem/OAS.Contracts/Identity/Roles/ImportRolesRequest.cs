namespace OAS.Contracts.Identity.Roles;

public sealed record ImportRolesRequest(IReadOnlyList<RoleImportItem> Roles);
