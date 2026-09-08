using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Specifications;

public sealed class EmployeeCodeSpecification
    : Specification<Employee>
{
    public EmployeeCodeSpecification(
        string normalizedEmployeeCode,
        Guid? excludeEmployeeId = null)
    {
        Where(employee =>
            employee.NormalizedEmployeeCode ==
            normalizedEmployeeCode &&
            (!excludeEmployeeId.HasValue ||
             employee.Id != excludeEmployeeId.Value));
    }
}