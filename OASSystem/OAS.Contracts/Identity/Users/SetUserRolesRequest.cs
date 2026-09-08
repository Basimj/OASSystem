namespace OAS.Contracts.Identity.Users;

public sealed record SetUserRolesRequest(IReadOnlyList<Guid> RoleIds, string RowVersion);
