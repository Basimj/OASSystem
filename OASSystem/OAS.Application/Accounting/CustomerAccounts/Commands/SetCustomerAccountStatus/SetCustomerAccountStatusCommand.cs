using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.CustomerAccounts;

namespace OAS.Application.Accounting.CustomerAccounts.Commands.SetCustomerAccountStatus;

public sealed record SetCustomerAccountStatusCommand(Guid Id, SetCustomerAccountStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CustomerAccounts.Disable];
}
