using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Queries.GetBankAccountById;

public sealed record GetBankAccountByIdQuery(Guid Id)
    : GetEntityByIdQuery<BankAccount, Guid, BankAccountDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.BankAccounts.View];
}
