using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Departments;

namespace OAS.Client.Features.Employees.Services;

public sealed class DepartmentClientService(OasApiClient apiClient) : IDepartmentClientService
{
    public async Task<IReadOnlyList<DepartmentDto>> GetAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
        => await apiClient.GetAsync<IReadOnlyList<DepartmentDto>>($"api/departments?activeOnly={activeOnly.ToString().ToLowerInvariant()}", cancellationToken) ?? [];

    public Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => apiClient.GetAsync<DepartmentDto>($"api/departments/{id:D}", cancellationToken);

    public Task<ApiCallResult<DepartmentDto>> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostResultAsync<CreateDepartmentRequest, DepartmentDto>("api/departments", request, cancellationToken);

    public Task<ApiCallResult<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
        => apiClient.PutResultAsync<UpdateDepartmentRequest, DepartmentDto>($"api/departments/{id:D}", request, cancellationToken);

    public Task<ApiCallResult<DepartmentDto>> SetStatusAsync(Guid id, SetDepartmentStatusRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostResultAsync<SetDepartmentStatusRequest, DepartmentDto>($"api/departments/{id:D}/status", request, cancellationToken);
}
