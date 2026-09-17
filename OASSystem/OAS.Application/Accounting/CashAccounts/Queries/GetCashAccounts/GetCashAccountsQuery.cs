using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Queries.GetCashAccounts;

public sealed record GetCashAccountsQuery(PageRequest Request)
    : GetEntityPageQuery<CashAccount, Guid, CashAccountDto>(Request),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashAccounts.View];
}
