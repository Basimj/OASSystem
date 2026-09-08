namespace OAS.Application.Features.Employees.Abstractions;

public interface IEmployeeExcelExporter
{
    byte[] Export(
        IReadOnlyCollection<EmployeeExportRow> employees);
}

public sealed record EmployeeExportRow(
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? JobTitle,
    DateTime? HireDate,
    bool IsSalesEmployee,
    bool IsTechnician,
    bool IsCommissionEligible,
    bool IsActive,
    string? Notes);