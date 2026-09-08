namespace OAS.Contracts.Features.Employees.Import;

public sealed record EmployeeImportPreviewDto(
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyCollection<EmployeeImportErrorDto> Errors)
{
    public bool IsValid =>
        TotalRows > 0 &&
        InvalidRows == 0 &&
        Errors.Count == 0;
}