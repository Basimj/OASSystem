using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Features.Employees.Images;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Features.Employees.Images.Commands.SetEmployeeImage;

public sealed class SetEmployeeImageCommandHandler(
    IRepository<Employee, Guid> employees,
    IEmployeeImageStore store,
    IUnitOfWork unitOfWork) : IRequestHandler<SetEmployeeImageCommand>
{
    public async Task Handle(SetEmployeeImageCommand request, CancellationToken cancellationToken)
    {
        var employee = await employees.GetForUpdateAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);


        EmployeeImagePolicy.EnsureValid(request.ContentType, request.Content);

        var oldPath = employee.Photo;
        var newPath = await store.SaveAsync(employee.Id, request.ContentType, request.Content, cancellationToken);

        try
        {
            employee.SetPhoto(newPath);
            employees.Update(employee);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteAsync(store, newPath, cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(oldPath) &&
            !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
        {
            await TryDeleteAsync(store, oldPath, cancellationToken);
        }
    }

    private static async Task TryDeleteAsync(
        IEmployeeImageStore store,
        string? relativePath,
        CancellationToken cancellationToken)
    {
        try
        {
            await store.DeleteAsync(relativePath, cancellationToken);
        }
        catch
        {
            // The database path remains valid even if cleanup of an obsolete file fails.
            // A failed cleanup must not roll back an otherwise successful employee photo update.
        }
    }
}
