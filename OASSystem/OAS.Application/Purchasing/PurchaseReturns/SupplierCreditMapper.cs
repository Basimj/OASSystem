using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.PurchaseReturns;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.PurchaseReturns;

internal static class SupplierCreditMapper
{
    public static SupplierCreditApplicationDto ToDto(SupplierCreditApplication entity) =>
        new(
            entity.Id,
            entity.PurchaseReturnId,
            entity.PurchaseInvoiceId,
            entity.SupplierId,
            entity.AppliedSourceBaseAmount,
            entity.AppliedTargetBaseAmount,
            entity.AppliedInvoiceCurrencyAmount,
            entity.AppliedAtUtc,
            entity.AppliedBy,
            (SupplierCreditApplicationStatus)(byte)entity.Status,
            entity.IdempotencyKey,
            entity.ReversedAtUtc,
            entity.ReversedBy,
            entity.ReversalReason,
            PurchasingRowVersion.Encode(entity.RowVersion));
}
