using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.SupplierAccounts;

namespace OAS.Application.Accounting.SupplierAccounts.Commands.SetSupplierAccountStatus;

public sealed record SetSupplierAccountStatusCommand(Guid Id, SetSupplierAccountStatusRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
        [AccountingPermissions.SupplierAccounts.Disable];
}
