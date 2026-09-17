using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CustomerAccounts.Commands.CreateCustomerAccount;

public sealed record CreateCustomerAccountCommand(CreateCustomerAccountRequest Data)
    : CreateEntityCommand<CustomerAccount, Guid, CreateCustomerAccountRequest>(Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CustomerAccounts.Create];
}
