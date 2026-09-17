using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Commands.CreateCashAccount;

public sealed record CreateCashAccountCommand(CreateCashAccountRequest Data)
    : CreateEntityCommand<CashAccount, Guid, CreateCashAccountRequest>(Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashAccounts.Create];
}
