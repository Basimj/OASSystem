using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Images.Queries.GetEmployeeImage;

public sealed class GetEmployeeImageQueryHandler(
    IReadRepository<Employee, Guid> employees,
    IEmployeeImageStore store)
    : IRequestHandler<GetEmployeeImageQuery, EmployeeImageData?>
{
    public async Task<EmployeeImageData?> Handle(GetEmployeeImageQuery request, CancellationToken cancellationToken)
    {
        var employee = await employees.GetByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null || string.IsNullOrWhiteSpace(employee.Photo)) return null;
        return await store.GetAsync(employee.Photo, cancellationToken);
    }
}
