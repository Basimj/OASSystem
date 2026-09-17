using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Queries.GetExpenseTypeById;

public sealed record GetExpenseTypeByIdQuery(Guid Id)
    : GetEntityByIdQuery<ExpenseType, Guid, ExpenseTypeDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.ExpenseTypes.View];
}
