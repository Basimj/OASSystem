using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Features.Employees.Entities;

public sealed class Employee : AuditableEntity<Guid>
{
    private Employee()
    {
    }

    private Employee(
        Guid id,
        string employeeCode,
        string firstName,
        string lastName,
        string? phone,
        string? jobTitle,
        DateOnly? hireDate,
        string? notes,
        bool isSalesperson,
        bool isTechnician,
        bool isCommissionEligible,
        bool isActive,
        Guid? userAccountId)
    {
        if (id == Guid.Empty)
            throw new DomainException("Employee id is required.");

        Id = id;

        UpdateDetails(
            employeeCode,
            firstName,
            lastName,
            phone,
            jobTitle,
            hireDate,
            notes);

        SetCapabilities(
            isSalesperson,
            isTechnician,
            isCommissionEligible);

        SetUserAccount(userAccountId);
        SetActive(isActive);
    }

    public string EmployeeCode { get; private set; } = string.Empty;

    public string NormalizedEmployeeCode { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? JobTitle { get; private set; }

    public DateOnly? HireDate { get; private set; }

    public string? Notes { get; private set; }

    public bool IsSalesperson { get; private set; }

    public bool IsTechnician { get; private set; }

    public bool IsCommissionEligible { get; private set; }

    public bool IsActive { get; private set; }

    public Guid? UserAccountId { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public string DisplayName =>
        string.Join(
            ' ',
            new[] { FirstName, LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

    public static Employee Create(
        Guid id,
        string employeeCode,
        string firstName,
        string lastName,
        string? phone,
        string? jobTitle,
        DateOnly? hireDate,
        string? notes,
        bool isSalesperson,
        bool isTechnician,
        bool isCommissionEligible,
        bool isActive,
        Guid? userAccountId)
    {
        return new Employee(
            id,
            employeeCode,
            firstName,
            lastName,
            phone,
            jobTitle,
            hireDate,
            notes,
            isSalesperson,
            isTechnician,
            isCommissionEligible,
            isActive,
            userAccountId);
    }

    public void UpdateDetails(
        string employeeCode,
        string firstName,
        string lastName,
        string? phone,
        string? jobTitle,
        DateOnly? hireDate,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
            throw new DomainException("Employee code is required.");

        employeeCode = employeeCode.Trim();

        if (employeeCode.Length > 32)
            throw new DomainException(
                "Employee code cannot exceed 32 characters.");

        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required.");

        firstName = firstName.Trim();

        if (firstName.Length > 100)
            throw new DomainException(
                "First name cannot exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required.");

        lastName = lastName.Trim();

        if (lastName.Length > 100)
            throw new DomainException(
                "Last name cannot exceed 100 characters.");

        phone = NormalizeOptional(phone);

        if (phone is not null && phone.Length > 32)
            throw new DomainException(
                "Phone cannot exceed 32 characters.");

        jobTitle = NormalizeOptional(jobTitle);

        if (jobTitle is not null && jobTitle.Length > 100)
            throw new DomainException(
                "Job title cannot exceed 100 characters.");

        notes = NormalizeOptional(notes);

        if (notes is not null && notes.Length > 1000)
            throw new DomainException(
                "Notes cannot exceed 1000 characters.");

        EmployeeCode = employeeCode;
        NormalizedEmployeeCode = NormalizeEmployeeCode(employeeCode);

        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        JobTitle = jobTitle;
        HireDate = hireDate;
        Notes = notes;
    }

    public void SetCapabilities(
        bool isSalesperson,
        bool isTechnician,
        bool isCommissionEligible)
    {
        IsSalesperson = isSalesperson;
        IsTechnician = isTechnician;
        IsCommissionEligible = isCommissionEligible;
    }

    public void SetUserAccount(Guid? userAccountId)
    {
        UserAccountId = userAccountId;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    private static string NormalizeEmployeeCode(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}