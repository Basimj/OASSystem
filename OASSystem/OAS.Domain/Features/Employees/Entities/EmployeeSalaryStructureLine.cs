using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Entities;

public sealed class EmployeeSalaryStructureLine : AuditableEntity<Guid>
{
    private EmployeeSalaryStructureLine() { }

    private EmployeeSalaryStructureLine(Guid id, Guid employeeSalaryStructureId, Guid salaryComponentId, decimal amount, decimal? percentage, string componentCodeSnapshot, string componentNameSnapshot, SalaryComponentType componentTypeSnapshot, SalaryCalculationMethod calculationMethodSnapshot, bool isBasicSalarySnapshot, string? debitPostingRoleSnapshot, string? creditPostingRoleSnapshot)
    {
        if (id == Guid.Empty) throw new DomainException("Salary structure line id is required.");
        if (employeeSalaryStructureId == Guid.Empty) throw new DomainException("Salary structure is required.");
        if (salaryComponentId == Guid.Empty) throw new DomainException("Salary component is required.");
        if (amount < 0) throw new DomainException("Salary amount cannot be negative.");
        if (percentage.HasValue && percentage.Value <= 0) throw new DomainException("Salary percentage must be greater than zero.");
        if (calculationMethodSnapshot == SalaryCalculationMethod.PercentageOfBasic && !percentage.HasValue) throw new DomainException("Percentage is required for percentage-of-basic components.");
        if (calculationMethodSnapshot != SalaryCalculationMethod.PercentageOfBasic && percentage.HasValue) throw new DomainException("Percentage is only valid for percentage-of-basic components.");
        if (string.IsNullOrWhiteSpace(componentCodeSnapshot) || string.IsNullOrWhiteSpace(componentNameSnapshot)) throw new DomainException("Salary component snapshot is required.");
        Id = id;
        EmployeeSalaryStructureId = employeeSalaryStructureId;
        SalaryComponentId = salaryComponentId;
        Amount = amount;
        Percentage = percentage;
        ComponentCodeSnapshot = componentCodeSnapshot.Trim();
        ComponentNameSnapshot = componentNameSnapshot.Trim();
        ComponentTypeSnapshot = componentTypeSnapshot;
        CalculationMethodSnapshot = calculationMethodSnapshot;
        IsBasicSalarySnapshot = isBasicSalarySnapshot;
        DebitPostingRoleSnapshot = Normalize(debitPostingRoleSnapshot);
        CreditPostingRoleSnapshot = Normalize(creditPostingRoleSnapshot);
    }

    public Guid EmployeeSalaryStructureId { get; private set; }
    public Guid SalaryComponentId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? Percentage { get; private set; }
    public string ComponentCodeSnapshot { get; private set; } = string.Empty;
    public string ComponentNameSnapshot { get; private set; } = string.Empty;
    public SalaryComponentType ComponentTypeSnapshot { get; private set; }
    public SalaryCalculationMethod CalculationMethodSnapshot { get; private set; }
    public bool IsBasicSalarySnapshot { get; private set; }
    public string? DebitPostingRoleSnapshot { get; private set; }
    public string? CreditPostingRoleSnapshot { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static EmployeeSalaryStructureLine Create(Guid id, Guid employeeSalaryStructureId, SalaryComponent component, decimal amount, decimal? percentage)
    {
        ArgumentNullException.ThrowIfNull(component);
        if (!component.IsActive) throw new DomainException("Inactive salary components cannot be added to a salary structure.");
        return new(id, employeeSalaryStructureId, component.Id, amount, percentage, component.ComponentCode, component.NameAr, component.ComponentType, component.CalculationMethod, component.IsBasicSalary, component.DebitPostingRole, component.CreditPostingRole);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
