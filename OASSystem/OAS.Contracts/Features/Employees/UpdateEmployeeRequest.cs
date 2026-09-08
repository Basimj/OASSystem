namespace OAS.Contracts.Features.Employees;

public sealed record UpdateEmployeeRequest(
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
    Guid? UserAccountId,
    string RowVersion);