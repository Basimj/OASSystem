using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Mapping;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.CreateExpenseType;

public sealed class CreateExpenseTypeCommandHandler(
    IRepository<ExpenseType, Guid> repository,
    ExpenseTypeMapper mapper)
    : IRequestHandler<CreateExpenseTypeCommand, ExpenseType>
{
    public async Task<ExpenseType> Handle(
        CreateExpenseTypeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
