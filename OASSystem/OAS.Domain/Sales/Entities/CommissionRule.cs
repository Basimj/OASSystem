using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Sales.Entities;

public sealed class CommissionRule : AuditableEntity<Guid>
{
    private CommissionRule() { }

    private CommissionRule(Guid id, string code, string name, Guid? employeeId, decimal ratePercent,
        DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        if (id == Guid.Empty) throw new DomainException("Commission rule id is required.");
        Id = id;
        Code = NormalizeRequired(code, 40, "Commission rule code");
        Name = NormalizeRequired(name, 160, "Commission rule name");
        if (employeeId == Guid.Empty) throw new DomainException("Commission employee id cannot be empty.");
        EmployeeId = employeeId;
        SetRate(ratePercent);
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new DomainException("Commission rule effective-to date cannot precede effective-from date.");
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid? EmployeeId { get; private set; }
    public decimal RatePercent { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static CommissionRule Create(Guid id, string code, string name, Guid? employeeId, decimal ratePercent,
        DateOnly effectiveFrom, DateOnly? effectiveTo = null) =>
        new(id, code, name, employeeId, ratePercent, effectiveFrom, effectiveTo);

    public bool AppliesTo(Guid employeeId, DateOnly date) => IsActive &&
        (!EmployeeId.HasValue || EmployeeId.Value == employeeId) &&
        EffectiveFrom <= date && (!EffectiveTo.HasValue || EffectiveTo.Value >= date);

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
