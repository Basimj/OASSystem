using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Compensation;

namespace OAS.Client.Features.Employees.Services;

public sealed class EmployeeCompensationClientService(OasApiClient apiClient) : IEmployeeCompensationClientService
{
    public async Task<IReadOnlyList<SalaryComponentDto>> GetComponentsAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
        => await apiClient.GetAsync<IReadOnlyList<SalaryComponentDto>>($"api/employees/salary-components?activeOnly={activeOnly.ToString().ToLowerInvariant()}", cancellationToken) ?? [];

    public Task<ApiCallResult<SalaryComponentDto>> CreateComponentAsync(CreateSalaryComponentRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostResultAsync<CreateSalaryComponentRequest, SalaryComponentDto>("api/employees/salary-components", request, cancellationToken);

    public Task<ApiCallResult<SalaryComponentDto>> UpdateComponentAsync(Guid id, UpdateSalaryComponentRequest request, CancellationToken cancellationToken = default)
        => apiClient.PutResultAsync<UpdateSalaryComponentRequest, SalaryComponentDto>($"api/employees/salary-components/{id:D}", request, cancellationToken);

    public async Task<IReadOnlyList<EmployeeSalaryStructureDto>> GetStructuresAsync(Guid employeeId, CancellationToken cancellationToken = default)
        => await apiClient.GetAsync<IReadOnlyList<EmployeeSalaryStructureDto>>($"api/employees/{employeeId:D}/salary-structures", cancellationToken) ?? [];

    public Task<ApiCallResult<EmployeeSalaryStructureDto>> CreateStructureAsync(Guid employeeId, CreateEmployeeSalaryStructureRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostResultAsync<CreateEmployeeSalaryStructureRequest, EmployeeSalaryStructureDto>($"api/employees/{employeeId:D}/salary-structures", request, cancellationToken);

    public Task<ApiCallResult<EmployeeSalaryStructureDto>> UpdateStructureAsync(Guid id, UpdateEmployeeSalaryStructureRequest request, CancellationToken cancellationToken = default)
        => apiClient.PutResultAsync<UpdateEmployeeSalaryStructureRequest, EmployeeSalaryStructureDto>($"api/employees/salary-structures/{id:D}", request, cancellationToken);

    public Task<ApiCallResult<EmployeeSalaryStructureDto>> ActivateStructureAsync(Guid id, SalaryStructureLifecycleRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostResultAsync<SalaryStructureLifecycleRequest, EmployeeSalaryStructureDto>($"api/employees/salary-structures/{id:D}/activate", request, cancellationToken);

    public Task<ApiCallResult<EmployeeSalaryStructureDto>> CancelStructureAsync(Guid id, SalaryStructureLifecycleRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostResultAsync<SalaryStructureLifecycleRequest, EmployeeSalaryStructureDto>($"api/employees/salary-structures/{id:D}/cancel", request, cancellationToken);
}
