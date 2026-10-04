using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Contracts;

namespace OAS.Client.Features.Employees.Services;

public sealed class EmployeeContractClientService(OasApiClient apiClient) : IEmployeeContractClientService
{
    public async Task<IReadOnlyList<EmployeeContractDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
        => await apiClient.GetAsync<IReadOnlyList<EmployeeContractDto>>($"api/employees/{employeeId:D}/contracts", cancellationToken) ?? [];

    public Task<EmployeeContractDto?> GetByIdAsync(Guid contractId, CancellationToken cancellationToken = default)
        => apiClient.GetAsync<EmployeeContractDto>($"api/employees/contracts/{contractId:D}", cancellationToken);

    public Task<ApiCallResult<EmployeeContractDto>> CreateAsync(Guid employeeId, CreateEmployeeContractRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostResultAsync<CreateEmployeeContractRequest, EmployeeContractDto>($"api/employees/{employeeId:D}/contracts", request, cancellationToken);

    public Task<ApiCallResult<EmployeeContractDto>> UpdateAsync(Guid contractId, UpdateEmployeeContractRequest request, CancellationToken cancellationToken = default)
        => apiClient.PutResultAsync<UpdateEmployeeContractRequest, EmployeeContractDto>($"api/employees/contracts/{contractId:D}", request, cancellationToken);

    public Task<ApiCallResult<EmployeeContractDto>> ActivateAsync(Guid contractId, ContractLifecycleRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostResultAsync<ContractLifecycleRequest, EmployeeContractDto>($"api/employees/contracts/{contractId:D}/activate", request, cancellationToken);

    public Task<ApiCallResult<EmployeeContractDto>> TerminateAsync(Guid contractId, TerminateEmployeeContractRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostResultAsync<TerminateEmployeeContractRequest, EmployeeContractDto>($"api/employees/contracts/{contractId:D}/terminate", request, cancellationToken);

    public Task<ApiCallResult<EmployeeContractDto>> CancelAsync(Guid contractId, ContractLifecycleRequest request, CancellationToken cancellationToken = default)
        => apiClient.PostResultAsync<ContractLifecycleRequest, EmployeeContractDto>($"api/employees/contracts/{contractId:D}/cancel", request, cancellationToken);
}
