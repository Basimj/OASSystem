using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Identity.Users;

namespace OAS.Application.Identity.Users.Mapping;

internal static class UserMapping
{
    public static UserSummaryDto ToSummary(IdentityUserRecord record, DateTimeOffset nowUtc) => new(
        record.User.Id,
        record.User.UserName,
        record.User.DisplayName,
        record.User.Email,
        record.User.IsActive,
        record.User.IsSuperAdmin,
        record.User.MustChangePassword,
        record.User.IsLockedOut(nowUtc),
        record.User.LockoutEndUtc,
        record.User.LastLoginAtUtc,
        record.Roles.Select(x => x.Name).OrderBy(x => x).ToArray(),
        record.User.CreatedAtUtc,
        Convert.ToBase64String(record.User.RowVersion));

    public static UserDetailsDto ToDetails(IdentityUserRecord record, DateTimeOffset nowUtc) => new(
        record.User.Id,
        record.User.UserName,
        record.User.FirstName,
        record.User.LastName,
        record.User.DisplayName,
        record.User.Email,
        record.User.IsActive,
        record.User.IsSuperAdmin,
        record.User.MustChangePassword,
        record.User.AccessFailedCount,
        record.User.LockoutEndUtc,
        record.User.IsLockedOut(nowUtc),
        record.User.LastLoginAtUtc,
        record.Roles.Select(x => x.Id).ToArray(),
        record.Roles.Select(x => x.Name).OrderBy(x => x).ToArray(),
        record.User.CreatedAtUtc,
        record.User.CreatedBy,
        record.User.LastModifiedAtUtc,
        record.User.LastModifiedBy,
        Convert.ToBase64String(record.User.RowVersion));
}
