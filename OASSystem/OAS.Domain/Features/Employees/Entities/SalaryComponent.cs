using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Entities;

public sealed class SalaryComponent : AuditableEntity<Guid>
{
    private SalaryComponent() { }

    private SalaryComponent(Guid id, string componentCode, string nameAr, string? nameEn, SalaryComponentType componentType, SalaryCalculationMethod calculationMethod, bool isBasicSalary, bool isRecurring, bool isTaxable, bool isActive, string? debitPostingRole, string? creditPostingRole, int displayOrder, string? notes)
    {
        if (id == Guid.Empty) throw new DomainException("Salary component id is required.");
        if (string.IsNullOrWhiteSpace(componentCode)) throw new DomainException("Salary component code is required.");
        Id = id;
        ComponentCode = componentCode.Trim();
        if (ComponentCode.Length > 32) throw new DomainException("Salary component code cannot exceed 32 characters.");
        Update(nameAr, nameEn, componentType, calculationMethod, isBasicSalary, isRecurring, isTaxable, isActive, debitPostingRole, creditPostingRole, displayOrder, notes);
    }

    public string ComponentCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public SalaryComponentType ComponentType { get; private set; }
    public SalaryCalculationMethod CalculationMethod { get; private set; }
    public bool IsBasicSalary { get; private set; }
    public bool IsRecurring { get; private set; }
    public bool IsTaxable { get; private set; }
    public bool IsActive { get; private set; }
    public string? DebitPostingRole { get; private set; }
    public string? CreditPostingRole { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static SalaryComponent Create(Guid id, string componentCode, string nameAr, string? nameEn, SalaryComponentType componentType, SalaryCalculationMethod calculationMethod, bool isBasicSalary, bool isRecurring, bool isTaxable, bool isActive, string? debitPostingRole, string? creditPostingRole, int displayOrder, string? notes)
        => new(id, componentCode, nameAr, nameEn, componentType, calculationMethod, isBasicSalary, isRecurring, isTaxable, isActive, debitPostingRole, creditPostingRole, displayOrder, notes);

    public void Update(string nameAr, string? nameEn, SalaryComponentType componentType, SalaryCalculationMethod calculationMethod, bool isBasicSalary, bool isRecurring, bool isTaxable, bool isActive, string? debitPostingRole, string? creditPostingRole, int displayOrder, string? notes)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("Arabic salary component name is required.");
        nameAr = nameAr.Trim();
        if (nameAr.Length > 150) throw new DomainException("Arabic salary component name cannot exceed 150 characters.");
        if (!Enum.IsDefined(componentType)) throw new DomainException("Salary component type is invalid.");
        if (!Enum.IsDefined(calculationMethod)) throw new DomainException("Salary calculation method is invalid.");
        if (isBasicSalary && componentType != SalaryComponentType.Earning) throw new DomainException("Basic salary must be an earning component.");
        if (displayOrder < 0) throw new DomainException("Display order cannot be negative.");
        nameEn = Normalize(nameEn);
        debitPostingRole = Normalize(debitPostingRole);
        creditPostingRole = Normalize(creditPostingRole);
        notes = Normalize(notes);
        if (nameEn is { Length: > 150 }) throw new DomainException("English salary component name cannot exceed 150 characters.");
        if (debitPostingRole is { Length: > 50 } || creditPostingRole is { Length: > 50 }) throw new DomainException("Posting role cannot exceed 50 characters.");
        if (notes is { Length: > 500 }) throw new DomainException("Salary component notes cannot exceed 500 characters.");

        NameAr = nameAr;
        NameEn = nameEn;
        ComponentType = componentType;
        CalculationMethod = calculationMethod;
        IsBasicSalary = isBasicSalary;
        IsRecurring = isRecurring;
        IsTaxable = isTaxable;
        IsActive = isActive;
        DebitPostingRole = debitPostingRole;
        CreditPostingRole = creditPostingRole;
        DisplayOrder = displayOrder;
        Notes = notes;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
