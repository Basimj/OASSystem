using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class ProductCategory : AuditableEntity<Guid>
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid ProductTypeId { get; private set; }
    public bool IsActive { get; private set; }

    private ProductCategory() { }

    public ProductCategory(string code, string name, Guid productTypeId)
    {
        Id = Guid.NewGuid();
        Code = code;
        Name = name;
        ProductTypeId = productTypeId;
        IsActive = true;
    }

    public void UpdateDetails(string code, string name, Guid productTypeId, bool isActive)
    {
        Code = code;
        Name = name;
        ProductTypeId = productTypeId;
        IsActive = isActive;
    }
}
