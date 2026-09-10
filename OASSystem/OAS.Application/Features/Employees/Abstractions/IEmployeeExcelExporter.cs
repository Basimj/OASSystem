namespace OAS.Application.Features.Employees.Abstractions;

public interface IEmployeeExcelExporter
{
    byte[] Export(IReadOnlyCollection<EmployeeExportRow> employees);
}

public sealed record EmployeeExportRow(
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Email,
    string? Country,
    string? Governorate,
    string? City,
    string? PostalCode,
    string? ResidentialAddress,
    string JobTitle,
    DateTime? HireDate,
    bool IsCommissionEligible,
    bool IsActive);
