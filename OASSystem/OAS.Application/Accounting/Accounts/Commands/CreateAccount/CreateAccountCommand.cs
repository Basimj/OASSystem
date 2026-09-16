using OAS.Application.Accounting.Authorization;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.Accounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Commands.CreateAccount;

public record CreateAccountCommand(
    CreateAccountRequest Data)
    : CreateEntityCommand<Account, Guid, CreateAccountRequest>(Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Accounts.Create];
}