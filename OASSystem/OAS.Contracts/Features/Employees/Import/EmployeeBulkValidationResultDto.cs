namespace OAS.Contracts.Features.Employees.Import;

public sealed record EmployeeBulkValidationResultDto(
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyCollection<EmployeeBulkValidationErrorDto> Errors)
{
    public bool IsValid =>
        TotalRows > 0 &&
        InvalidRows == 0 &&
        Errors.Count == 0;
}