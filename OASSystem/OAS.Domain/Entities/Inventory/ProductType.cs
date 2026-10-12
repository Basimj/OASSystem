using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class ProductType : AuditableEntity<Guid>
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? SystemKey { get; private set; }
    public bool IsActive { get; private set; }

    private ProductType() { }

    public ProductType(string code, string name, string? systemKey = null)
    {
        Id = Guid.NewGuid();
        Code = code;
        Name = name;
        SystemKey = systemKey;
        IsActive = true;
    }

    public void UpdateDetails(string code, string name, bool isActive)
    {
        Code = code;
        Name = name;
        IsActive = isActive;
    }
}
