namespace OAS.Contracts.Features.Employees.Import;

public sealed record EmployeeImportErrorDto(
    int RowNumber,
    string Column,
    string Message);