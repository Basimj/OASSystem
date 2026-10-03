using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Services;

public sealed class DepartmentHierarchyValidator(IReadRepository<Department, Guid> departments)
{
    public async Task ValidateParentAsync(Guid departmentId, Guid? parentDepartmentId, CancellationToken cancellationToken)
    {
        if (parentDepartmentId is null) return;
        if (parentDepartmentId == departmentId)
            throw new ConflictException("department_parent_self", "A department cannot be its own parent.");
        var parent = await departments.GetByIdAsync(parentDepartmentId.Value, cancellationToken)
            ?? throw new ConflictException("department_parent_not_found", "The selected parent department does not exist.");
        if (!parent.IsActive)
            throw new ConflictException("department_parent_inactive", "The selected parent department is inactive.");
        var visited = new HashSet<Guid> { departmentId };
        var current = parent;
        while (true)
        {
            if (!visited.Add(current.Id))
                throw new ConflictException("department_hierarchy_cycle", "The selected parent would create a department hierarchy cycle.");
            if (current.ParentDepartmentId is not Guid next) break;
            if (next == departmentId)
                throw new ConflictException("department_hierarchy_cycle", "The selected parent would create a department hierarchy cycle.");
            var nextDepartment = await departments.GetByIdAsync(next, cancellationToken);
            if (nextDepartment is null)
                break;
            current = nextDepartment;
        }
    }
}
