using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.BankAccounts;

namespace OAS.Application.Accounting.BankAccounts.Commands.SetBankAccountStatus;

public sealed record SetBankAccountStatusCommand(Guid Id, SetBankAccountStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.BankAccounts.Disable];
}
