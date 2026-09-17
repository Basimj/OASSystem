using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Expenses;

namespace OAS.Application.Accounting.Expenses.Commands.CreateExpense;

public sealed record CreateExpenseCommand(CreateExpenseRequest Data)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Expenses.Create];
}
