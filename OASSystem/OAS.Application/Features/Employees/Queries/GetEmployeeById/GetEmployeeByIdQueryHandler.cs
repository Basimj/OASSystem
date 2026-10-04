using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Mapping;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler(
    IReadRepository<Employee, Guid> employeeRepository,
    IReadRepository<JobTitle, Guid> jobTitleRepository,
    IReadRepository<UserAccount, Guid> userRepository,
    IReadRepository<Department, Guid> departmentRepository)
    : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);
        var jobTitle = await jobTitleRepository.GetByIdAsync(employee.JobTitleId, cancellationToken)
            ?? throw new NotFoundException(nameof(JobTitle), employee.JobTitleId);
        Department? department = employee.DepartmentId is Guid departmentId
            ? await departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            : null;
        Employee? manager = employee.ManagerEmployeeId is Guid managerId
            ? await employeeRepository.GetByIdAsync(managerId, cancellationToken)
            : null;
        UserAccount? user = employee.UserAccountId is Guid userId
            ? await userRepository.GetByIdAsync(userId, cancellationToken)
            : null;
        return EmployeeMapping.ToDto(employee, jobTitle, department, manager, user);
    }
}
