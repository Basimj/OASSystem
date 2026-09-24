using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class ProductType : AuditableEntity<Guid>
{
    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public string? SystemKey { get; private set; }
    public bool IsActive { get; private set; }

    private ProductType()
    {
    }

    public ProductType(string code, string nameAr, string? nameEn = null, string? systemKey = null)
    {
        Id = Guid.NewGuid();
        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        SystemKey = systemKey;
        IsActive = true;
    }

    public void UpdateDetails(string code, string nameAr, string? nameEn, bool isActive)
    {
        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        IsActive = isActive;
    }
}
