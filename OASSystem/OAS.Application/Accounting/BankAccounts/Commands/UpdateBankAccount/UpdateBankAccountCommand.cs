using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Commands.UpdateBankAccount;

public sealed record UpdateBankAccountCommand(Guid Id, UpdateBankAccountRequest Data)
    : UpdateEntityCommand<BankAccount, Guid, UpdateBankAccountRequest>(Id, Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.BankAccounts.Edit];
}
