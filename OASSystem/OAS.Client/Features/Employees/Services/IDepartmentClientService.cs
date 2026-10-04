using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Departments;

namespace OAS.Client.Features.Employees.Services;

public interface IDepartmentClientService
{
    Task<IReadOnlyList<DepartmentDto>> GetAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiCallResult<DepartmentDto>> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<DepartmentDto>> SetStatusAsync(Guid id, SetDepartmentStatusRequest request, CancellationToken cancellationToken = default);
}
