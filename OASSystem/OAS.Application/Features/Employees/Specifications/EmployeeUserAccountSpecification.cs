using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Specifications;

public sealed class EmployeeUserAccountSpecification
    : Specification<Employee>
{
    public EmployeeUserAccountSpecification(
        Guid userAccountId,
        Guid? excludeEmployeeId = null)
    {
        Where(employee =>
            employee.UserAccountId == userAccountId &&
            (!excludeEmployeeId.HasValue ||
             employee.Id != excludeEmployeeId.Value));
    }
}