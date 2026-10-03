using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Abstractions;

public interface IEmployeeSalaryStructureRepository
{
    Task<EmployeeSalaryStructure?> GetByIdAsync(Guid id, bool tracking, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeSalaryStructure>> ListForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeSalaryStructure?> GetActiveForEmployeeAsync(Guid employeeId, bool tracking, CancellationToken cancellationToken = default);
    Task AddAsync(EmployeeSalaryStructure entity, CancellationToken cancellationToken = default);
    void Update(EmployeeSalaryStructure entity);
}
