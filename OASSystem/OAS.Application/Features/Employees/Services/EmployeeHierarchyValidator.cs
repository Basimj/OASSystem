using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Services;

public sealed class EmployeeHierarchyValidator(IReadRepository<Employee, Guid> employees)
{
    public async Task ValidateManagerAsync(Guid employeeId, Guid? managerEmployeeId, CancellationToken cancellationToken)
    {
        if (managerEmployeeId is null) return;
        if (managerEmployeeId == employeeId)
            throw new ConflictException("employee_manager_self", "An employee cannot be their own manager.");

        var manager = await employees.GetByIdAsync(managerEmployeeId.Value, cancellationToken)
            ?? throw new ConflictException("employee_manager_not_found", "The selected manager does not exist.");
        if (!manager.IsActive)
            throw new ConflictException("employee_manager_inactive", "The selected manager is inactive.");

        var visited = new HashSet<Guid> { employeeId };
        var current = manager;
        while (true)
        {
            if (!visited.Add(current.Id))
                throw new ConflictException("employee_manager_cycle", "The selected manager would create a reporting hierarchy cycle.");
            if (current.ManagerEmployeeId is not Guid parentId) break;
            if (parentId == employeeId)
                throw new ConflictException("employee_manager_cycle", "The selected manager would create a reporting hierarchy cycle.");
            var parentEmployee = await employees.GetByIdAsync(parentId, cancellationToken);
            if (parentEmployee is null)
                break;
            current = parentEmployee;
        }
    }
}
