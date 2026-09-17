using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.CRUD.Commands;
using OAS.Contracts.Accounting.SupplierAccounts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.SupplierAccounts.Commands.UpdateSupplierAccount;

public sealed record UpdateSupplierAccountCommand(Guid Id, UpdateSupplierAccountRequest Data)
    : UpdateEntityCommand<SupplierAccount, Guid, UpdateSupplierAccountRequest>(Id, Data),
      IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.SupplierAccounts.Edit];
}
