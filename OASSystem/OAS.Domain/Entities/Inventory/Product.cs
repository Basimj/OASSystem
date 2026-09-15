using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class Product : AuditableEntity<Guid>
{
    public string ProductCode { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }

    public Guid CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }

    public string ProductType { get; private set; } = null!;
    public string? Description { get; private set; }

    public bool IsStockItem { get; private set; }
    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private Product()
    {
    }

    public Product(
        string productCode,
        string nameAr,
        Guid categoryId,
        string productType,
        bool isStockItem = true,
        string? nameEn = null,
        Guid? brandId = null,
        string? description = null)
    {
        Id = Guid.NewGuid();

        ProductCode = productCode;
        NameAr = nameAr;
        NameEn = nameEn;
        CategoryId = categoryId;
        BrandId = brandId;
        ProductType = productType;
        Description = description;
        IsStockItem = isStockItem;
        IsActive = true;
    }
}