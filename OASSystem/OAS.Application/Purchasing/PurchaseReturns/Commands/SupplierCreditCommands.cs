using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.PurchaseReturns;

namespace OAS.Application.Purchasing.PurchaseReturns.Commands;

public sealed record ApplySupplierCreditCommand(Guid PurchaseReturnId, ApplySupplierCreditRequest Request)
    : ICommand<SupplierCreditApplicationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        PurchasingPermissions.Receipts.View,
        PurchasingPermissions.Invoices.View,
        AccountingPermissions.PaymentAllocations.Create
    ];
}

public sealed record ReverseSupplierCreditApplicationCommand(
    Guid PurchaseReturnId,
    Guid ApplicationId,
    ReverseSupplierCreditApplicationRequest Request)
    : ICommand<SupplierCreditApplicationDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        PurchasingPermissions.Receipts.View,
        PurchasingPermissions.Invoices.View,
        AccountingPermissions.PaymentAllocations.Edit
    ];
}
