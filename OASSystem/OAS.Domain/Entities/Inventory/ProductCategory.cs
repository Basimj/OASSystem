using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class ProductCategory : AuditableEntity<Guid>
{
    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }

    public Guid? ParentCategoryId { get; private set; }

    public bool IsActive { get; private set; }

    private ProductCategory()
    {
    }

    public ProductCategory(
        string code,
        string nameAr,
        string? nameEn = null,
        Guid? parentCategoryId = null)
    {
        Id = Guid.NewGuid();

        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        ParentCategoryId = parentCategoryId;
        IsActive = true;
    }

    public void UpdateDetails(
        string code,
        string nameAr,
        string? nameEn,
        Guid? parentCategoryId,
        bool isActive)
    {
        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        ParentCategoryId = parentCategoryId;
        IsActive = isActive;
    }
}