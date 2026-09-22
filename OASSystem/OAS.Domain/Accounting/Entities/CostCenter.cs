using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class CostCenter : AuditableEntity<Guid>
{
    private CostCenter()
    {
    }

    private CostCenter(
        Guid id,
        string code,
        string nameAr,
        string? nameEn,
        Guid? parentCostCenterId,
        bool isActive)
    {
        Id = id;
        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        ParentCostCenterId = parentCostCenterId;
        IsActive = isActive;
    }

    public string Code { get; private set; } = null!;

    public string NameAr { get; private set; } = null!;

    public string? NameEn { get; private set; }

    public Guid? ParentCostCenterId { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static CostCenter Create(
        Guid id,
        string code,
        string nameAr,
        string? nameEn,
        Guid? parentCostCenterId,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new ArgumentException("Arabic name is required.", nameof(nameAr));

        return new CostCenter(
            id,
            code.Trim(),
            nameAr.Trim(),
            string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            parentCostCenterId,
            isActive);
    }

    public void UpdateDetails(
        string code,
        string nameAr,
        string? nameEn,
        Guid? parentCostCenterId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new ArgumentException("Arabic name is required.", nameof(nameAr));

        Code = code.Trim();
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        ParentCostCenterId = parentCostCenterId;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
