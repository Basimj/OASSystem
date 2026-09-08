namespace OAS.Contracts.Features.Employees;

public sealed record CreateEmployeeRequest(
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? Phone,
    string? JobTitle,
    DateOnly? HireDate,
    string? Notes,
    bool IsSalesperson,
    bool IsTechnician,
    bool IsCommissionEligible,
    bool IsActive = true,
    Guid? UserAccountId = null);