using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.ValueObjects;

namespace OAS.Domain.Features.Employees.Entities;

public sealed class Employee : AuditableEntity<Guid>
{
    private Employee()
    {
        ContactInfo = ContactInfo.Empty;
    }

    private Employee(
        Guid id,
        int employeeNumber,
        string firstName,
        string lastName,
        ContactInfo contactInfo,
        Guid jobTitleId,
        DateOnly? hireDate,
        bool isCommissionEligible,
        bool isActive,
        Guid? userAccountId)
    {
        if (id == Guid.Empty) throw new DomainException("Employee id is required.");
        if (employeeNumber <= 0) throw new DomainException("Employee number is required.");
        Id = id;
        EmployeeNumber = employeeNumber;

        UpdateDetails(firstName, lastName, contactInfo, jobTitleId, hireDate, isCommissionEligible, isActive);

        if (userAccountId.HasValue)
            LinkUserAccount(userAccountId.Value);
    }

    public int EmployeeNumber { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public ContactInfo ContactInfo { get; private set; }
    public Guid JobTitleId { get; private set; }
    public DateOnly? HireDate { get; private set; }
    public bool IsCommissionEligible { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? UserAccountId { get; private set; }
    public string? Photo { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public string DisplayName =>
        string.Join(' ', new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public string EmployeeCode => EmployeeCodeFormatter.Format(EmployeeNumber);

    public static Employee Create(
        Guid id,
        int employeeNumber,
        string firstName,
        string lastName,
        ContactInfo contactInfo,
        Guid jobTitleId,
        DateOnly? hireDate,
        bool isCommissionEligible,
        bool isActive,
        Guid? userAccountId = null) =>
        new(id, employeeNumber, firstName, lastName, contactInfo, jobTitleId, hireDate, isCommissionEligible, isActive, userAccountId);

    public void UpdateDetails(
        string firstName,
        string lastName,
        ContactInfo contactInfo,
        Guid jobTitleId,
        DateOnly? hireDate,
        bool isCommissionEligible,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required.");
        firstName = firstName.Trim();
        if (firstName.Length > 100)
            throw new DomainException("First name cannot exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required.");
        lastName = lastName.Trim();
        if (lastName.Length > 100)
            throw new DomainException("Last name cannot exceed 100 characters.");

        if (jobTitleId == Guid.Empty)
            throw new DomainException("Job title is required.");

        FirstName = firstName;
        LastName = lastName;
        ContactInfo = contactInfo ?? throw new DomainException("Contact information is required.");
        JobTitleId = jobTitleId;
        HireDate = hireDate;
        IsCommissionEligible = isCommissionEligible;
        IsActive = isActive;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void SetUserAccountLink(Guid? userAccountId)
    {
        if (UserAccountId.HasValue)
        {
            if (userAccountId != UserAccountId)
                throw new DomainException("The linked user account cannot be changed or removed.");
            return;
        }

        if (userAccountId.HasValue)
            LinkUserAccount(userAccountId.Value);
    }

    public void LinkUserAccount(Guid userAccountId)
    {
        if (userAccountId == Guid.Empty)
            throw new DomainException("User account id is required.");
        if (UserAccountId.HasValue)
            throw new DomainException("Employee is already linked to a user account.");

        UserAccountId = userAccountId;
    }

    public void SetPhoto(string? relativePath)
    {
        Photo = NormalizeOptional(relativePath);
        if (Photo is not null && Photo.Length > 512)
            throw new DomainException("Employee photo path cannot exceed 512 characters.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
