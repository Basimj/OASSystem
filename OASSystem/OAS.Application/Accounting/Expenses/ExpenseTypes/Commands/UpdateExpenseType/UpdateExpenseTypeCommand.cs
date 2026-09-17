using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.UpdateExpenseType;

public sealed record UpdateExpenseTypeCommand(Guid Id, UpdateExpenseTypeRequest Data)
    : UpdateEntityCommand<ExpenseType, Guid, UpdateExpenseTypeRequest>(Id, Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.ExpenseTypes.Edit];
}
