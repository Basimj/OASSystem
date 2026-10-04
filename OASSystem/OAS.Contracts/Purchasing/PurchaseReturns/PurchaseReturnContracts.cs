using OAS.Contracts.Common.Pagination;

namespace OAS.Contracts.Purchasing.PurchaseReturns;

public enum PurchaseReturnStatus : byte
{
    Draft = 1,
    Confirmed = 2,
    Posted = 3,
    Cancelled = 4
}

public sealed record CreatePurchaseReturnLineRequest(Guid PurchaseReceiptLineId, decimal Quantity);
public sealed record CreatePurchaseReturnRequest(
    Guid PurchaseReceiptId,
    Guid? PurchaseInvoiceId,
    DateOnly ReturnDate,
    DateOnly PostingDate,
    string? Reason,
    IReadOnlyList<CreatePurchaseReturnLineRequest> Lines);
public sealed record PurchaseReturnActionRequest(string RowVersion);
public sealed record CancelPurchaseReturnRequest(string RowVersion, string? Reason);

public sealed record PurchaseReturnLineDto(
    Guid Id,
    int LineNumber,
    Guid PurchaseReceiptLineId,
    Guid? PurchaseInvoiceLineId,
    Guid ProductVariantId,
    decimal Quantity,
    decimal BaseQuantity,
    decimal ReceiptUnitCostBase,
    decimal ReceiptCostBaseAmount,
    decimal SupplierNetBaseAmount,
    decimal SupplierTaxBaseAmount,
    decimal SupplierGrossBaseAmount,
    decimal? InventoryUnitCostBase,
    decimal? InventoryCostBaseAmount);

public sealed record PurchaseReturnDto(
    Guid Id,
    string ReturnCode,
    Guid PurchaseReceiptId,
    Guid? PurchaseInvoiceId,
    Guid SupplierId,
    Guid WarehouseId,
    DateOnly ReturnDate,
    DateOnly PostingDate,
    PurchaseReturnStatus Status,
    decimal ReceiptCostBaseAmount,
    decimal SupplierNetBaseAmount,
    decimal SupplierTaxBaseAmount,
    decimal SupplierGrossBaseAmount,
    decimal InventoryCostBaseAmount,
    decimal PurchasePriceVarianceBaseAmount,
    string? Reason,
    Guid? JournalEntryId,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? PostedAt,
    DateTimeOffset? CancelledAt,
    string RowVersion,
    IReadOnlyList<PurchaseReturnLineDto> Lines);

public sealed record PurchaseReturnPostingResultDto(
    Guid PurchaseReturnId,
    Guid JournalEntryId,
    IReadOnlyList<Guid> InventoryTransactionIds);
