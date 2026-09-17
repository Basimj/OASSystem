using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Commands.UpdateCashAccount;

public sealed record UpdateCashAccountCommand(Guid Id, UpdateCashAccountRequest Data)
    : UpdateEntityCommand<CashAccount, Guid, UpdateCashAccountRequest>(Id, Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashAccounts.Edit];
}
