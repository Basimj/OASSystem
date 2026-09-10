namespace OAS.Contracts.Features.Employees.Import;

public sealed record EmployeeBulkValidationRowDto(
    int RowNumber,
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    string? Country,
    string? Governorate,
    string? City,
    string? PostalCode,
    string? ResidentialAddress,
    string? JobTitle,
    DateOnly? HireDate,
    bool IsCommissionEligible,
    bool IsActive);
