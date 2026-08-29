using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Identity.Entities;

public sealed class Role : AuditableEntity<Guid>
{
    private Role() { }

    private Role(Guid id, string name, string displayName, bool isSystem)
    {
        Id = id;
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Role name is required.");
        Name = name.Trim();
        NormalizedName = UserAccount.Normalize(Name);
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Name : displayName.Trim();
        IsSystem = isSystem;
    }

    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }

    public static Role Create(Guid id, string name, string displayName, bool isSystem = false)
    {
        if (id == Guid.Empty) throw new DomainException("Role id is required.");
        return new Role(id, name, displayName, isSystem);
    }
}
