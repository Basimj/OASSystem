using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.SupplierAccounts.Commands.CreateSupplierAccount;

public sealed record CreateSupplierAccountCommand(CreateSupplierAccountRequest Data)
    : CreateEntityCommand<SupplierAccount, Guid, CreateSupplierAccountRequest>(Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.SupplierAccounts.Create];
}
