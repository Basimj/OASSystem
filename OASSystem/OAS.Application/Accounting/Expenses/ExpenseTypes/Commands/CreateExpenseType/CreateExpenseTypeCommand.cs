using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.CreateExpenseType;

public sealed record CreateExpenseTypeCommand(CreateExpenseTypeRequest Data)
    : CreateEntityCommand<ExpenseType, Guid, CreateExpenseTypeRequest>(Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.ExpenseTypes.Create];
}
