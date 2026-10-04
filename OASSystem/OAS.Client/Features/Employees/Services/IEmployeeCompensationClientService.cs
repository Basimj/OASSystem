using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Compensation;

namespace OAS.Client.Features.Employees.Services;

public interface IEmployeeCompensationClientService
{
    Task<IReadOnlyList<SalaryComponentDto>> GetComponentsAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<ApiCallResult<SalaryComponentDto>> CreateComponentAsync(CreateSalaryComponentRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<SalaryComponentDto>> UpdateComponentAsync(Guid id, UpdateSalaryComponentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeSalaryStructureDto>> GetStructuresAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeSalaryStructureDto>> CreateStructureAsync(Guid employeeId, CreateEmployeeSalaryStructureRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeSalaryStructureDto>> UpdateStructureAsync(Guid id, UpdateEmployeeSalaryStructureRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeSalaryStructureDto>> ActivateStructureAsync(Guid id, SalaryStructureLifecycleRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<EmployeeSalaryStructureDto>> CancelStructureAsync(Guid id, SalaryStructureLifecycleRequest request, CancellationToken cancellationToken = default);
}
