namespace OAS.Contracts.Features.Employees;

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    string? Country,
    string? Governorate,
    string? City,
    string? PostalCode,
    string? ResidentialAddress,
    Guid JobTitleId,
    DateOnly? HireDate,
    bool IsCommissionEligible,
    bool IsActive,
    Guid? UserAccountId,
    string RowVersion);
