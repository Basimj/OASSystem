using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Commands.SetEmployeeStatus;

public sealed class SetEmployeeStatusCommandHandler(IRepository<Employee, Guid> employeeRepository)
    : IRequestHandler<SetEmployeeStatusCommand, Guid>
{
    public async Task<Guid> Handle(SetEmployeeStatusCommand request, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetForUpdateAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var incomingRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!incomingRowVersion.SequenceEqual(employee.RowVersion))
            throw new ConcurrencyException("The employee was changed by another operation. Reload it and try again.");

        employee.SetActive(request.Request.IsActive);
        employeeRepository.Update(employee);
        return employee.Id;
    }
}
