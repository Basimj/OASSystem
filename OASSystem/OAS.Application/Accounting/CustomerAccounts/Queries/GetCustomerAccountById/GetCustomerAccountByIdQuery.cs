using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.CustomerAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CustomerAccounts.Queries.GetCustomerAccountById;

public sealed record GetCustomerAccountByIdQuery(Guid Id)
    : GetEntityByIdQuery<CustomerAccount, Guid, CustomerAccountDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CustomerAccounts.View];
}
