using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class Product : AuditableEntity<Guid>
{
    public string ProductCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }
    public string? Description { get; private set; }
    public bool IsStockItem { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Product() { }

    public Product(
        string productCode,
        string name,
        Guid categoryId,
        bool isStockItem = true,
        Guid? brandId = null,
        string? description = null)
    {
        Id = Guid.NewGuid();
        ProductCode = productCode;
        Name = name;
        CategoryId = categoryId;
        BrandId = brandId;
        Description = description;
        IsStockItem = isStockItem;
        IsActive = true;
    }

    public void UpdateDetails(
        string productCode,
        string name,
        Guid categoryId,
        Guid? brandId,
        string? description,
        bool isStockItem,
        bool isActive)
    {
        ProductCode = productCode;
        Name = name;
        CategoryId = categoryId;
        BrandId = brandId;
        Description = description;
        IsStockItem = isStockItem;
        IsActive = isActive;
    }
}
