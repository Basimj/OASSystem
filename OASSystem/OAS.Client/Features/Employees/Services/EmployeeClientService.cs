using Microsoft.AspNetCore.Components.Forms;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Errors;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Features.Employees;
using OAS.Contracts.Features.Employees.Import;
using OAS.Contracts.Features.Employees.JobTitles;

namespace OAS.Client.Features.Employees.Services;

public sealed class EmployeeClientService(OasApiClient apiClient) : IEmployeeClientService
{
    private const string Endpoint = "api/employees";

    public async Task<PagedResult<EmployeeDto>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<PagedResult<EmployeeDto>>($"{Endpoint}{BuildQuery(request)}", cancellationToken)
        ?? new PagedResult<EmployeeDto>();

    public async Task<EmployeeDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<EmployeeDto>($"{Endpoint}/{id}", cancellationToken)
        ?? throw new ApiClientException(new ApiError { Status = 404, Code = "not_found", Message = "Employee was not found." });

    public async Task<EmployeeNumberReservationDto> ReserveEmployeeNumberAsync(CancellationToken cancellationToken = default) =>
        (await apiClient.PostAsync<object, EmployeeNumberReservationDto>($"{Endpoint}/number/reserve", new { }, cancellationToken))
        ?? throw new ApiClientException(new ApiError { Status = 500, Code = "employee_number_reservation_failed", Message = "Unable to reserve an employee number." });

    public Task<ApiCallResult<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<CreateEmployeeRequest, EmployeeDto>(Endpoint, request, cancellationToken);

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default) =>
        await apiClient.PutAsync<UpdateEmployeeRequest, EmployeeDto>($"{Endpoint}/{id}", request, cancellationToken)
        ?? throw new ApiClientException(new ApiError { Status = 404, Code = "not_found", Message = "Employee was not found." });

    public Task<ApiCallResult<EmployeeDto>> SetStatusAsync(Guid id, SetEmployeeStatusRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<SetEmployeeStatusRequest, EmployeeDto>($"{Endpoint}/{id}/status", request, cancellationToken);

    public async Task<IReadOnlyList<JobTitleDto>> GetJobTitlesAsync(bool activeOnly = false, CancellationToken cancellationToken = default) =>
        await apiClient.GetAsync<JobTitleDto[]>($"api/job-titles?activeOnly={activeOnly.ToString().ToLowerInvariant()}", cancellationToken) ?? [];

    public Task<ApiCallResult<JobTitleDto>> CreateJobTitleAsync(CreateJobTitleRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<CreateJobTitleRequest, JobTitleDto>("api/job-titles", request, cancellationToken);

    public Task<ApiCallResult<JobTitleDto>> UpdateJobTitleAsync(Guid id, UpdateJobTitleRequest request, CancellationToken cancellationToken = default) =>
        apiClient.PutResultAsync<UpdateJobTitleRequest, JobTitleDto>($"api/job-titles/{id:D}", request, cancellationToken);

    public async Task<ApiCallResult<bool>> UploadImageAsync(
        Guid id, byte[] content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream(content, writable: false);
        return await apiClient.UploadFilePutResultAsync($"{Endpoint}/{id:D}/image", stream, fileName, contentType, cancellationToken);
    }

    public Task<ApiCallResult<bool>> RemoveImageAsync(Guid id, CancellationToken cancellationToken = default) =>
        apiClient.DeleteResultAsync($"{Endpoint}/{id:D}/image", cancellationToken);

    public async Task<EmployeeImportPreviewDto> ValidateImportAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        return await apiClient.UploadFileAsync<EmployeeImportPreviewDto>(
            $"{Endpoint}/import/validate", stream, file.Name, cancellationToken: cancellationToken)
            ?? new EmployeeImportPreviewDto(0, 0, 0, []);
    }

    public async Task<EmployeeImportResultDto> ImportAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        return await apiClient.UploadFileAsync<EmployeeImportResultDto>(
            $"{Endpoint}/import", stream, file.Name, cancellationToken: cancellationToken)
            ?? new EmployeeImportResultDto(0);
    }

    public Task<ApiCallResult<EmployeeBulkValidationResultDto>> ValidateBulkAsync(
        EmployeeBulkValidationRequest request,
        CancellationToken cancellationToken = default) =>
        apiClient.PostResultAsync<EmployeeBulkValidationRequest, EmployeeBulkValidationResultDto>(
            $"{Endpoint}/bulk/validate", request, cancellationToken);

    public Task<byte[]> DownloadTemplateAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetFileAsync($"{Endpoint}/template", cancellationToken);

    public Task<byte[]> ExportAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetFileAsync("api/employees/export", cancellationToken);

    private static string BuildQuery(PageRequest request)
    {
        var normalized = request.Normalize();
        var parts = new List<string>
        {
            $"pageNumber={normalized.PageNumber}",
            $"pageSize={normalized.PageSize}",
            $"sortDirection={normalized.SortDirection}"
        };
        if (!string.IsNullOrWhiteSpace(normalized.Search))
            parts.Add($"search={Uri.EscapeDataString(normalized.Search)}");
        if (!string.IsNullOrWhiteSpace(normalized.SortBy))
            parts.Add($"sortBy={Uri.EscapeDataString(normalized.SortBy)}");
        return "?" + string.Join("&", parts);
    }
}
