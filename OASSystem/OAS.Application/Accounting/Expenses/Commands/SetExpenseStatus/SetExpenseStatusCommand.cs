using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.Expenses;

namespace OAS.Application.Accounting.Expenses.Commands.SetExpenseStatus;

public sealed record SetExpenseStatusCommand(Guid Id, SetExpenseStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Expenses.Edit];
}
