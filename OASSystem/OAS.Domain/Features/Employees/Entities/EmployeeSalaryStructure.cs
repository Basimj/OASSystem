using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Entities;

public sealed class EmployeeSalaryStructure : AuditableEntity<Guid>
{
    private readonly List<EmployeeSalaryStructureLine> _lines = [];

    private EmployeeSalaryStructure() { }

    private EmployeeSalaryStructure(Guid id, string structureCode, Guid employeeId, Guid? contractId, Guid currencyId, DateOnly effectiveFrom, DateOnly? effectiveTo, string? notes)
    {
        if (id == Guid.Empty) throw new DomainException("Salary structure id is required.");
        if (string.IsNullOrWhiteSpace(structureCode)) throw new DomainException("Salary structure code is required.");
        if (employeeId == Guid.Empty) throw new DomainException("Employee is required.");
        if (currencyId == Guid.Empty) throw new DomainException("Currency is required.");
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom) throw new DomainException("Salary structure effective-to date cannot be before effective-from date.");
        Id = id;
        StructureCode = structureCode.Trim();
        if (StructureCode.Length > 40) throw new DomainException("Salary structure code cannot exceed 40 characters.");
        EmployeeId = employeeId;
        ContractId = contractId;
        CurrencyId = currencyId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Notes = Normalize(notes);
        Status = SalaryStructureStatus.Draft;
    }

    public string StructureCode { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid? ContractId { get; private set; }
    public Guid CurrencyId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public SalaryStructureStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<EmployeeSalaryStructureLine> Lines => _lines.AsReadOnly();

    public static EmployeeSalaryStructure Create(Guid id, string structureCode, Guid employeeId, Guid? contractId, Guid currencyId, DateOnly effectiveFrom, DateOnly? effectiveTo, string? notes)
        => new(id, structureCode, employeeId, contractId, currencyId, effectiveFrom, effectiveTo, notes);

    public void UpdateDraft(Guid? contractId, Guid currencyId, DateOnly effectiveFrom, DateOnly? effectiveTo, string? notes)
    {
        EnsureDraft();
        if (currencyId == Guid.Empty) throw new DomainException("Currency is required.");
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom) throw new DomainException("Salary structure effective-to date cannot be before effective-from date.");
        ContractId = contractId;
        CurrencyId = currencyId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Notes = Normalize(notes);
    }

    public void ReplaceLines(IEnumerable<EmployeeSalaryStructureLine> lines)
    {
        EnsureDraft();
        var incoming = lines?.ToArray() ?? throw new DomainException("Salary structure lines are required.");
        if (incoming.Length == 0) throw new DomainException("Salary structure must contain at least one line.");
        if (incoming.Select(x => x.SalaryComponentId).Distinct().Count() != incoming.Length) throw new DomainException("Salary structure cannot contain duplicate components.");
        if (incoming.Count(x => x.IsBasicSalarySnapshot) != 1) throw new DomainException("Salary structure must contain exactly one basic salary component.");
        _lines.Clear();
        _lines.AddRange(incoming);
    }

    public void Activate(string? actor, DateTimeOffset atUtc)
    {
        EnsureDraft();
        if (_lines.Count == 0) throw new DomainException("Salary structure must contain at least one line.");
        if (_lines.Count(x => x.IsBasicSalarySnapshot) != 1) throw new DomainException("Salary structure must contain exactly one basic salary component.");
        Status = SalaryStructureStatus.Active;
        ApprovedBy = Normalize(actor);
        ApprovedAtUtc = atUtc;
    }

    public void Supersede(DateOnly effectiveTo)
    {
        if (Status != SalaryStructureStatus.Active) throw new DomainException("Only active salary structures can be superseded.");
        if (effectiveTo < EffectiveFrom) throw new DomainException("Salary structure end date cannot be before its start date.");
        if (!EffectiveTo.HasValue || effectiveTo < EffectiveTo.Value)
            EffectiveTo = effectiveTo;

        Status = SalaryStructureStatus.Superseded;
    }

    public void Cancel()
    {
        EnsureDraft();
        Status = SalaryStructureStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != SalaryStructureStatus.Draft) throw new DomainException("Only draft salary structures can be changed.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
