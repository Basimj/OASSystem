using Microsoft.AspNetCore.Components.Forms;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;
using OAS.Contracts.Features.Employees.Import;
using OAS.Contracts.Features.Employees.JobTitles;

namespace OAS.Client.Features.Employees.Services;

public interface IEmployeeClientService
{
    Task<PagedResult<EmployeeDto>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeNumberReservationDto> ReserveEmployeeNumberAsync(CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeDto>> SetStatusAsync(Guid id, SetEmployeeStatusRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobTitleDto>> GetJobTitlesAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<ApiCallResult<JobTitleDto>> CreateJobTitleAsync(CreateJobTitleRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<JobTitleDto>> UpdateJobTitleAsync(Guid id, UpdateJobTitleRequest request, CancellationToken cancellationToken = default);

    Task<ApiCallResult<bool>> UploadImageAsync(Guid id, byte[] content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ApiCallResult<bool>> RemoveImageAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EmployeeImportPreviewDto> ValidateImportAsync(IBrowserFile file, CancellationToken cancellationToken = default);
    Task<EmployeeImportResultDto> ImportAsync(IBrowserFile file, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeBulkValidationResultDto>> ValidateBulkAsync(EmployeeBulkValidationRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadTemplateAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportAsync(CancellationToken cancellationToken = default);
}
