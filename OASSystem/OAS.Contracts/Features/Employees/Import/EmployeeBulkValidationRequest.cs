namespace OAS.Contracts.Features.Employees.Import;

public sealed record EmployeeBulkValidationRequest(
    IReadOnlyCollection<EmployeeBulkValidationRowDto> Rows);