namespace OAS.Contracts.Features.Employees;

public sealed record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Phone,
    string? JobTitle,
    DateOnly? HireDate,
    string? Notes,
    bool IsSalesperson,
    bool IsTechnician,
    bool IsCommissionEligible,
    bool IsActive,
    Guid? UserAccountId,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastModifiedAtUtc);