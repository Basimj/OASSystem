using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.UpdateExpenseType;

public sealed class UpdateExpenseTypeCommandHandler(
    IRepository<ExpenseType, Guid> repository,
    ExpenseTypeMapper mapper)
    : IRequestHandler<UpdateExpenseTypeCommand, ExpenseType>
{
    public async Task<ExpenseType> Handle(
        UpdateExpenseTypeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ExpenseType), request.Id);
        }

        if (!string.Equals(entity.Code, request.Data.Code.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                "accounting_expense_type_code_immutable",
                "The expense type code cannot be changed after creation.");
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The expense type has been modified by another user.");
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
