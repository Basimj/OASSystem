using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public sealed class Unit : AuditableEntity<Guid>
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private Unit() { }

    public Unit(string code, string name)
    {
        Id = Guid.NewGuid();
        Code = code;
        Name = name;
        IsActive = true;
    }

    public void UpdateDetails(string code, string name, bool isActive)
    {
        Code = code;
        Name = name;
        IsActive = isActive;
    }
}
