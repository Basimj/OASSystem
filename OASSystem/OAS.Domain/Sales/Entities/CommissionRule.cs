using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Commissions;
using OAS.Domain.Sales.Enums;

namespace OAS.Domain.Sales.Entities;

public sealed class CommissionRule : AuditableEntity<Guid>
{
    private CommissionRule() { }

    private CommissionRule(Guid id, string code, string name, Guid? employeeId, Guid? productVariantId,
        Guid? productCategoryId, SalesLineType? saleType, int priority, CommissionBasisKind basisKind,
        decimal ratePercent, DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        if (id == Guid.Empty) throw new DomainException("Commission rule id is required.");
        if (employeeId == Guid.Empty || productVariantId == Guid.Empty || productCategoryId == Guid.Empty)
            throw new DomainException("Optional commission rule identifiers cannot be empty GUID values.");
        if (priority < 0 || priority > 10000) throw new DomainException("Commission priority must be between 0 and 10000.");
        if (saleType.HasValue && !Enum.IsDefined(saleType.Value)) throw new DomainException("Commission sale type is invalid.");
        if (!Enum.IsDefined(basisKind)) throw new DomainException("Commission basis kind is invalid.");
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Commission rule effective-to date cannot precede effective-from date.");

        Id = id;
        Code = NormalizeRequired(code, 40, "Commission rule code");
        Name = NormalizeRequired(name, 160, "Commission rule name");
        EmployeeId = employeeId;
        ProductVariantId = productVariantId;
        ProductCategoryId = productCategoryId;
        SaleType = saleType;
        Priority = priority;
        BasisKind = basisKind;
        SetRate(ratePercent);
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid? EmployeeId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public Guid? ProductCategoryId { get; private set; }
    public SalesLineType? SaleType { get; private set; }
    public int Priority { get; private set; }
    public CommissionBasisKind BasisKind { get; private set; } = CommissionBasisKind.NetAfterDiscountExcludingTax;
    public decimal RatePercent { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static CommissionRule Create(Guid id, string code, string name, Guid? employeeId, decimal ratePercent,
        DateOnly effectiveFrom, DateOnly? effectiveTo = null) =>
        new(id, code, name, employeeId, null, null, null, 0,
            CommissionBasisKind.NetAfterDiscountExcludingTax, ratePercent, effectiveFrom, effectiveTo);

    public static CommissionRule Create(Guid id, string code, string name, Guid? employeeId, Guid? productVariantId,
        Guid? productCategoryId, SalesLineType? saleType, int priority, CommissionBasisKind basisKind,
        decimal ratePercent, DateOnly effectiveFrom, DateOnly? effectiveTo = null) =>
        new(id, code, name, employeeId, productVariantId, productCategoryId, saleType, priority, basisKind,
            ratePercent, effectiveFrom, effectiveTo);

    public bool AppliesTo(Guid employeeId, DateOnly date) => AppliesTo(employeeId, null, null, null, date);

    public bool AppliesTo(Guid employeeId, Guid? productVariantId, Guid? productCategoryId, SalesLineType? saleType, DateOnly date) =>
        IsActive &&
        (!EmployeeId.HasValue || EmployeeId.Value == employeeId) &&
        (!ProductVariantId.HasValue || ProductVariantId == productVariantId) &&
        (!ProductCategoryId.HasValue || ProductCategoryId == productCategoryId) &&
        (!SaleType.HasValue || SaleType == saleType) &&
        EffectiveFrom <= date && (!EffectiveTo.HasValue || EffectiveTo.Value >= date);

    public int Specificity =>
        (EmployeeId.HasValue ? 8 : 0) +
        (ProductVariantId.HasValue ? 4 : 0) +
        (ProductCategoryId.HasValue ? 2 : 0) +
        (SaleType.HasValue ? 1 : 0);

    public void Deactivate() => IsActive = false;

    private void SetRate(decimal value)
    {
        if (value < 0m || value > 100m) throw new DomainException("Commission rate must be between 0 and 100.");
        RatePercent = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeRequired(string value, int max, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"{name} is required.");
        value = value.Trim();
        if (value.Length > max) throw new DomainException($"{name} cannot exceed {max} characters.");
        return value;
    }
}
