using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.SetExpenseTypeStatus;

public sealed class SetExpenseTypeStatusCommandHandler(
    IRepository<ExpenseType, Guid> repository)
    : IRequestHandler<SetExpenseTypeStatusCommand>
{
    public async Task Handle(
        SetExpenseTypeStatusCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ExpenseType), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The expense type has been modified by another user.");
        }

        entity.SetActive(request.Request.IsActive);
        repository.Update(entity);
    }
}
