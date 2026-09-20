using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public class Warehouse : AuditableEntity<Guid>
{
    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public string? Description { get; private set; }

    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Warehouse()
    {
    }

    public Warehouse(
        string code,
        string nameAr,
        string? nameEn = null,
        string? description = null,
        bool isDefault = false)
    {
        Id = Guid.NewGuid();

        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        Description = description;
        IsDefault = isDefault;
        IsActive = true;
    }

    public void UpdateDetails(
        string nameAr,
        string? nameEn,
        string? description,
        bool isDefault,
        bool isActive)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        Description = description;
        IsDefault = isDefault;
        IsActive = isActive;
    }
}