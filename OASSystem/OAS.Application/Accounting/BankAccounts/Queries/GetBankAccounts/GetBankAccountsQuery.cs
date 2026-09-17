using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.BankAccounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Queries.GetBankAccounts;

public sealed record GetBankAccountsQuery(PageRequest Request)
    : GetEntityPageQuery<BankAccount, Guid, BankAccountDto>(Request),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.BankAccounts.View];
}
