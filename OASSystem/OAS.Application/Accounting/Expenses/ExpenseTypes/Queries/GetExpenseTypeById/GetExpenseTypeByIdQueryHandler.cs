using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Queries.GetExpenseTypeById;

public sealed class GetExpenseTypeByIdQueryHandler(
    IReadRepository<ExpenseType, Guid> repository,
    ExpenseTypeMapper mapper)
    : IRequestHandler<GetExpenseTypeByIdQuery, ExpenseTypeDto>
{
    public async Task<ExpenseTypeDto> Handle(
        GetExpenseTypeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ExpenseType), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
