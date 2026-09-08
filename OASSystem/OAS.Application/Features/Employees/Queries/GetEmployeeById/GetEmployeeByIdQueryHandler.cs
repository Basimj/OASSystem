using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Mapping;
using OAS.Contracts.Features.Employees;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler(
    IReadRepository<Employee, Guid> employeeRepository)
    : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdAsync(
            request.EmployeeId,
            cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException(
                nameof(Employee),
                request.EmployeeId);
        }

        return EmployeeMapping.ToDto(employee);
    }
}