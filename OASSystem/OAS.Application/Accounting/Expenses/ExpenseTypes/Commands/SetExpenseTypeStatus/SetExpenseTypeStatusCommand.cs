using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Expenses;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.SetExpenseTypeStatus;

public sealed record SetExpenseTypeStatusCommand(Guid Id, SetExpenseTypeStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.ExpenseTypes.Disable];
}
