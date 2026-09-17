using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Expenses.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.Queries.GetExpenseById;

public sealed class GetExpenseByIdQueryHandler(
    IReadRepository<Expense, Guid> repository,
    ExpenseMapper mapper)
    : IRequestHandler<GetExpenseByIdQuery, ExpenseDto>
{
    public async Task<ExpenseDto> Handle(
        GetExpenseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(Expense), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
