using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CashAccounts;

namespace OAS.Application.Accounting.CashAccounts.Commands.SetCashAccountStatus;

public sealed record SetCashAccountStatusCommand(Guid Id, SetCashAccountStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashAccounts.Disable];
}
