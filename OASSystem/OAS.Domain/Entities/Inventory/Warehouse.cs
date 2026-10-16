using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class Warehouse : AuditableEntity<Guid>
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public bool AllowsSalesAllocation { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Warehouse() { }

    public Warehouse(string code, string name, string? description = null, bool isDefault = false, bool allowsSalesAllocation = true)
    {
        Id = Guid.NewGuid();
        Code = code;
        Name = name;
        Description = description;
        IsDefault = isDefault;
        IsActive = true;
        AllowsSalesAllocation = allowsSalesAllocation;
    }

    public void UpdateDetails(string code, string name, string? description, bool isDefault, bool isActive) =>
        UpdateDetails(code, name, description, isDefault, isActive, AllowsSalesAllocation);

    public void UpdateDetails(string code, string name, string? description, bool isDefault, bool isActive, bool allowsSalesAllocation)
    {
        Code = code;
        Name = name;
        Description = description;
        IsDefault = isDefault;
        IsActive = isActive;
        AllowsSalesAllocation = allowsSalesAllocation;
    }
}
