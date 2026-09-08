using OAS.Contracts.Features.Employees.Import;

namespace OAS.Application.Features.Employees.Abstractions;

public interface IEmployeeExcelReader
{
    Task<IReadOnlyList<EmployeeImportRowDto>> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default);
}