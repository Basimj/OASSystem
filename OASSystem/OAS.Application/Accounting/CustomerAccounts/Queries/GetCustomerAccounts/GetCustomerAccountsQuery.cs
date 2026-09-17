using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CustomerAccounts.Queries.GetCustomerAccounts;

public sealed record GetCustomerAccountsQuery(PageRequest Request)
    : GetEntityPageQuery<CustomerAccount, Guid, CustomerAccountDto>(Request),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CustomerAccounts.View];
}
