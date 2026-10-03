using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.ValueObjects;

namespace OAS.Domain.Features.Employees.Entities;

public sealed class Employee : AuditableEntity<Guid>
{
    private Employee() => ContactInfo = ContactInfo.Empty;

    private Employee(Guid id, string employeeCode, string firstName, string lastName, ContactInfo contactInfo, Guid jobTitleId, Guid? departmentId, Guid? managerEmployeeId, DateOnly? hireDate, bool isSalesperson, bool isTechnician, bool isCommissionEligible, bool isActive, Guid? userAccountId)
    {
        if (id == Guid.Empty) throw new DomainException("Employee id is required.");
        if (string.IsNullOrWhiteSpace(employeeCode)) throw new DomainException("Employee code is required.");
        employeeCode = employeeCode.Trim();
        if (employeeCode.Length > 32) throw new DomainException("Employee code cannot exceed 32 characters.");
        Id = id;
        EmployeeCode = employeeCode;
        UpdatePersonalDetails(firstName, lastName, contactInfo);
        UpdateEmploymentProfile(jobTitleId, departmentId, managerEmployeeId, hireDate);
        SetOperationalCapabilities(isSalesperson, isTechnician, isCommissionEligible);
        IsActive = isActive;
        if (userAccountId.HasValue) LinkUserAccount(userAccountId.Value);
    }

    public string EmployeeCode { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public ContactInfo ContactInfo { get; private set; }
    public Guid JobTitleId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? ManagerEmployeeId { get; private set; }
    public DateOnly? HireDate { get; private set; }
    public bool IsSalesperson { get; private set; }
    public bool IsTechnician { get; private set; }
    public bool IsCommissionEligible { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? UserAccountId { get; private set; }
    public string? Photo { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public string DisplayName => string.Join(' ', new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public static Employee Create(Guid id, string employeeCode, string firstName, string lastName, ContactInfo contactInfo, Guid jobTitleId, Guid? departmentId, Guid? managerEmployeeId, DateOnly? hireDate, bool isSalesperson, bool isTechnician, bool isCommissionEligible, bool isActive, Guid? userAccountId = null)
        => new(id, employeeCode, firstName, lastName, contactInfo, jobTitleId, departmentId, managerEmployeeId, hireDate, isSalesperson, isTechnician, isCommissionEligible, isActive, userAccountId);

    public static Employee Create(Guid id, string employeeCode, string firstName, string lastName, ContactInfo contactInfo, Guid jobTitleId, DateOnly? hireDate, bool isCommissionEligible, bool isActive, Guid? userAccountId = null)
        => new(id, employeeCode, firstName, lastName, contactInfo, jobTitleId, null, null, hireDate, false, false, isCommissionEligible, isActive, userAccountId);


    public void UpdateDetails(string firstName, string lastName, ContactInfo contactInfo, Guid jobTitleId, DateOnly? hireDate, bool isCommissionEligible, bool isActive)
        => UpdateDetails(firstName, lastName, contactInfo, jobTitleId, DepartmentId, ManagerEmployeeId, hireDate, IsSalesperson, IsTechnician, isCommissionEligible, isActive);

    public void UpdateDetails(string firstName, string lastName, ContactInfo contactInfo, Guid jobTitleId, Guid? departmentId, Guid? managerEmployeeId, DateOnly? hireDate, bool isSalesperson, bool isTechnician, bool isCommissionEligible, bool isActive)
    {
        UpdatePersonalDetails(firstName, lastName, contactInfo);
        UpdateEmploymentProfile(jobTitleId, departmentId, managerEmployeeId, hireDate);
        SetOperationalCapabilities(isSalesperson, isTechnician, isCommissionEligible);
        IsActive = isActive;
    }

    public void UpdatePersonalDetails(string firstName, string lastName, ContactInfo contactInfo)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name is required.");
        firstName = firstName.Trim();
        if (firstName.Length > 100) throw new DomainException("First name cannot exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Last name is required.");
        lastName = lastName.Trim();
        if (lastName.Length > 100) throw new DomainException("Last name cannot exceed 100 characters.");
        FirstName = firstName;
        LastName = lastName;
        ContactInfo = contactInfo ?? throw new DomainException("Contact information is required.");
    }

    public void UpdateEmploymentProfile(Guid jobTitleId, Guid? departmentId, Guid? managerEmployeeId, DateOnly? hireDate)
    {
        if (jobTitleId == Guid.Empty) throw new DomainException("Job title is required.");
        if (managerEmployeeId == Id) throw new DomainException("An employee cannot be their own manager.");
        JobTitleId = jobTitleId;
        DepartmentId = departmentId;
        ManagerEmployeeId = managerEmployeeId;
        HireDate = hireDate;
    }

    public void SetOperationalCapabilities(bool isSalesperson, bool isTechnician, bool isCommissionEligible)
    {
        IsSalesperson = isSalesperson;
        IsTechnician = isTechnician;
        IsCommissionEligible = isCommissionEligible;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    public void SetUserAccountLink(Guid? userAccountId)
    {
        if (UserAccountId.HasValue)
        {
            if (userAccountId != UserAccountId) throw new DomainException("The linked user account cannot be changed or removed.");
            return;
        }
        if (userAccountId.HasValue) LinkUserAccount(userAccountId.Value);
    }

    public void LinkUserAccount(Guid userAccountId)
    {
        if (userAccountId == Guid.Empty) throw new DomainException("User account id is required.");
        if (UserAccountId.HasValue) throw new DomainException("Employee is already linked to a user account.");
        UserAccountId = userAccountId;
    }

    public void SetPhoto(string? relativePath)
    {
        Photo = NormalizeOptional(relativePath);
        if (Photo is not null && Photo.Length > 512) throw new DomainException("Employee photo path cannot exceed 512 characters.");
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
