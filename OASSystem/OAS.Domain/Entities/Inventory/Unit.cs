using OAS.Domain.Common;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Entities.Inventory;

public sealed class Unit : AuditableEntity<Guid>
{
    public string Code { get; private set; }
    public string NameAr { get; private set; }
    public string? NameEn { get; private set; }
    public bool IsActive { get; private set; }

    private Unit()
    {
        Code = null!;
        NameAr = null!;
    }

    public Unit(
        string code,
        string nameAr,
        string? nameEn = null)
    {
        Id = Guid.NewGuid();
        Code = code;
        NameAr = nameAr;
        NameEn = nameEn;
        IsActive = true;
    }
}