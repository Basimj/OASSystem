using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Abstractions;

public sealed record IdentityUserRecord(UserAccount User, IReadOnlyList<Role> Roles);
