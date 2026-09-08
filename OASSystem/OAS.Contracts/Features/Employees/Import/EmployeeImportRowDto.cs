namespace OAS.Contracts.Features.Employees.Import;

public sealed record EmployeeImportRowDto(
    int RowNumber,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? Phone,
    string? JobTitle,
    DateOnly? HireDate,
    string? Notes,
    string IsSalesperson,
    string IsTechnician,
    string IsCommissionEligible,
    string IsActive);