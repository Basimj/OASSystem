namespace OAS.Contracts.Features.Employees.Import;

public sealed record EmployeeBulkValidationRowDto(
    int RowNumber,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? Phone,
    string? JobTitle,
    DateOnly? HireDate,
    bool IsSalesperson,
    bool IsTechnician,
    bool IsCommissionEligible,
    bool IsActive);