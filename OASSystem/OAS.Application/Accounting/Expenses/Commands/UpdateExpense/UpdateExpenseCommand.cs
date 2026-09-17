using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Expenses;

namespace OAS.Application.Accounting.Expenses.Commands.UpdateExpense;

public sealed record UpdateExpenseCommand(Guid Id, UpdateExpenseRequest Data)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Expenses.Edit];
}
