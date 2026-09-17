using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Commands.CreateBankAccount;

public sealed record CreateBankAccountCommand(CreateBankAccountRequest Data)
    : CreateEntityCommand<BankAccount, Guid, CreateBankAccountRequest>(Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.BankAccounts.Create];
}
