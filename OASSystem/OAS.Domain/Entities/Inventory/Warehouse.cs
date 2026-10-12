using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class Warehouse : AuditableEntity<Guid>
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Warehouse() { }

    public Warehouse(string code, string name, string? description = null, bool isDefault = false)
    {
        Id = Guid.NewGuid();
        Code = code;
        Name = name;
        Description = description;
        IsDefault = isDefault;
        IsActive = true;
    }

    public void UpdateDetails(string code, string name, string? description, bool isDefault, bool isActive)
    {
        Code = code;
        Name = name;
        Description = description;
        IsDefault = isDefault;
        IsActive = isActive;
    }
}
