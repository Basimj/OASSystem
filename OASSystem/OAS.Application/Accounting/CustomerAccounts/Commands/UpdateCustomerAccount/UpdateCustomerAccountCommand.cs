using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CustomerAccounts.Commands.UpdateCustomerAccount;

public sealed record UpdateCustomerAccountCommand(Guid Id, UpdateCustomerAccountRequest Data)
    : UpdateEntityCommand<CustomerAccount, Guid, UpdateCustomerAccountRequest>(Id, Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CustomerAccounts.Edit];
}
