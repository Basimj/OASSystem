using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Contracts;

namespace OAS.Client.Features.Employees.Services;

public interface IEmployeeContractClientService
{
    Task<IReadOnlyList<EmployeeContractDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeContractDto?> GetByIdAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeContractDto>> CreateAsync(Guid employeeId, CreateEmployeeContractRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeContractDto>> UpdateAsync(Guid contractId, UpdateEmployeeContractRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeContractDto>> ActivateAsync(Guid contractId, ContractLifecycleRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeContractDto>> TerminateAsync(Guid contractId, TerminateEmployeeContractRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeContractDto>> CancelAsync(Guid contractId, ContractLifecycleRequest request, CancellationToken cancellationToken = default);
}
