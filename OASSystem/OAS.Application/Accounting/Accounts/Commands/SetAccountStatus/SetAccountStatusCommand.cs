using OAS.Application.Accounting.Authorization;
using OAS.Application.Abstractions.Messaging;
using OAS.Contracts.Accounting.Accounts;

namespace OAS.Application.Accounting.Accounts.Commands.SetAccountStatus;

public sealed record SetAccountStatusCommand(
    Guid AccountId,
    SetAccountStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Accounts.Disable];
}