using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Images.Commands.RemoveEmployeeImage;

public sealed class RemoveEmployeeImageCommandHandler(
    IRepository<Employee, Guid> employees,
    IEmployeeImageStore store,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveEmployeeImageCommand>
{
    public async Task Handle(RemoveEmployeeImageCommand request, CancellationToken cancellationToken)
    {
        var employee = await employees.GetForUpdateAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);


        var oldPath = employee.Photo;
        employee.SetPhoto(null);
        employees.Update(employee);

        // Persist the database change first. If deleting the physical file later fails,
        // the database will never point at a missing file; only an orphan file may remain.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(oldPath)) return;
        try
        {
            await store.DeleteAsync(oldPath, cancellationToken);
        }
        catch
        {
            // Best-effort cleanup. The employee no longer references this file.
        }
    }
}
