using OAS.Application.Accounting.Authorization;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.Accounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Commands.UpdateAccount;

public record UpdateAccountCommand(
    Guid Id,
    UpdateAccountRequest Data)
    : UpdateEntityCommand<Account, Guid, UpdateAccountRequest>(Id, Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Accounts.Edit];
}