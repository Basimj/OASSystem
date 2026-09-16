using OAS.Application.Accounting.Authorization;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.Accounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Queries.GetAccountById;

public record GetAccountByIdQuery(
    Guid Id)
    : GetEntityByIdQuery<Account, Guid, AccountDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Accounts.View];
}