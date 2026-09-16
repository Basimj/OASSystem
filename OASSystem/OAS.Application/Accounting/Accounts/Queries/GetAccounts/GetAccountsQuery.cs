using OAS.Application.Accounting.Authorization;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.Accounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Queries.GetAccounts;

public record GetAccountsQuery(
    PageRequest Request)
    : GetEntityPageQuery<Account, Guid, AccountDto>(Request),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.Accounts.View];
}