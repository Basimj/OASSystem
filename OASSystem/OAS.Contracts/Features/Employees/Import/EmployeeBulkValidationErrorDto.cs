namespace OAS.Contracts.Features.Employees.Import;

public sealed record EmployeeBulkValidationErrorDto(
    int RowNumber,
    string Field,
    string Message);