namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchasingInventoryPort
{
    Task IncreaseOnOrderAsync(Guid warehouseId, IReadOnlyList<PurchasingOnOrderLine> lines, Guid purchaseOrderId, DateOnly effectiveDate, CancellationToken cancellationToken = default);
    Task DecreaseOnOrderAsync(Guid warehouseId, IReadOnlyList<PurchasingOnOrderLine> lines, Guid purchaseOrderId, DateOnly effectiveDate, CancellationToken cancellationToken = default);
    Task ValidatePostingDateAsync(Guid warehouseId, DateOnly postingDate, CancellationToken cancellationToken = default);
    Task<PurchasingInventoryPostingResult> PostPurchaseReceiptAsync(PurchasingReceiptInventoryContext context, CancellationToken cancellationToken = default);
}
