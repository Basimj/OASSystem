using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.SupplierAccounts.Queries.GetSupplierAccountById;

public sealed record GetSupplierAccountByIdQuery(Guid Id)
    : GetEntityByIdQuery<SupplierAccount, Guid, SupplierAccountDto>(Id),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.SupplierAccounts.View];
}
