using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Queries.GetCashAccountById;

public sealed record GetCashAccountByIdQuery(Guid Id)
    : GetEntityByIdQuery<CashAccount, Guid, CashAccountDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.CashAccounts.View];
}
