using Microsoft.AspNetCore.Components.Forms;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;
using OAS.Contracts.Features.Employees.Import;

namespace OAS.Client.Features.Employees.Services;

public interface IEmployeeClientService
{
    Task<PagedResult<EmployeeDto>> GetPageAsync(
        PageRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiCallResult<EmployeeDto>> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto> UpdateAsync(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiCallResult<EmployeeDto>> SetStatusAsync(
        Guid id,
        SetEmployeeStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeImportPreviewDto> ValidateImportAsync(
        IBrowserFile file,
        CancellationToken cancellationToken = default);

    Task<EmployeeImportResultDto> ImportAsync(
        IBrowserFile file,
        CancellationToken cancellationToken = default);

    Task<ApiCallResult<EmployeeBulkValidationResultDto>>
        ValidateBulkAsync(
            EmployeeBulkValidationRequest request,
            CancellationToken cancellationToken = default);


    Task<byte[]> DownloadTemplateAsync(
    CancellationToken cancellationToken = default);

    Task<byte[]> ExportAsync(
    CancellationToken cancellationToken = default);

    Task<string> GetNextEmployeeCodeAsync(
    CancellationToken cancellationToken = default);

}