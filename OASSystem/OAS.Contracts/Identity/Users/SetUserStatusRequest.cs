namespace OAS.Contracts.Identity.Users;

public sealed record SetUserStatusRequest(bool IsActive, string RowVersion);
