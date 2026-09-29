using OAS.Contracts.Purchasing.Enums;

namespace OAS.Application.Purchasing.Abstractions;

public sealed record PurchasingSupplierSnapshot(Guid Id, string Code, string Name, bool IsActive, Guid AccountId);
public sealed record PurchasingWarehouseSnapshot(Guid Id, string Code, string Name, bool IsActive);
public sealed record PurchasingUnitSnapshot(Guid Id, string Code, string Name, bool IsActive);
public sealed record PurchasingCurrencySnapshot(Guid Id, string Code, bool IsActive);
public sealed record PurchasingCatalogDefaults(Guid CatalogItemId, Guid PurchaseUnitId, decimal UnitConversionFactor, decimal UnitPrice);
public sealed record PurchasingProductSnapshot(Guid VariantId, string? ProductCode, string ProductName, bool VariantIsActive, bool ProductIsActive, bool IsStockItem, Guid? DefaultUnitId);

public sealed record PurchasingOnOrderLine(Guid ProductVariantId, decimal BaseQuantity);
public sealed record PurchasingReceiptInventoryLine(Guid PurchaseReceiptLineId, Guid ProductVariantId, decimal BaseQuantity, decimal UnitCost, DateOnly? ExpiryDate, string? BatchCode);
public sealed record PurchasingReceiptInventoryContext(Guid PurchaseReceiptId, string ReceiptCode, Guid WarehouseId, DateOnly PostingDate, IReadOnlyList<PurchasingReceiptInventoryLine> Lines);
public sealed record PurchasingInventoryPostingResult(Guid InventoryTransactionId);

public sealed record PurchasingReceiptJournalLine(Guid PurchaseReceiptLineId, Guid ProductVariantId, decimal BaseAmount);
public sealed record PurchasingReceiptAccountingContext(Guid PurchaseReceiptId, string ReceiptCode, Guid SupplierId, DateOnly PostingDate, IReadOnlyList<PurchasingReceiptJournalLine> Lines);
public sealed record PurchasingInvoiceAccountingContext(Guid PurchaseInvoiceId, string PurchaseInvoiceCode, Guid SupplierId, DateOnly PostingDate, Guid CurrencyId, decimal ExchangeRate, decimal GrniBaseAmount, decimal PurchaseTaxBaseAmount, decimal PurchasePriceVarianceBaseAmount, decimal SupplierPayableBaseAmount);
public sealed record PurchasingAccountingPostingResult(Guid JournalEntryId);

public sealed record PurchaseMatchingTolerances(decimal QuantityTolerance, decimal PriceTolerance, decimal TaxTolerance);
public sealed record PurchaseReceiptMatchCandidate(
    Guid PurchaseReceiptLineId,
    string ReceiptCode,
    Guid PurchaseOrderLineId,
    Guid ProductVariantId,
    Guid SupplierId,
    Guid CurrencyId,
    decimal AcceptedQuantity,
    decimal AvailableQuantity,
    decimal ReceiptUnitCostBase,
    decimal PurchaseOrderUnitPrice,
    decimal PurchaseOrderTaxRate,
    PurchaseReceiptStatus ReceiptStatus);

public sealed record PurchaseMatchAllocationEvaluation(
    Guid PurchaseInvoiceLineId,
    Guid PurchaseReceiptLineId,
    string ReceiptCode,
    decimal MatchedQuantity,
    decimal MatchedNetAmount,
    decimal QuantityVariance,
    decimal PriceVarianceAmount,
    decimal TaxVarianceAmount,
    PurchaseMatchStatus Status,
    decimal PriceTolerance,
    decimal TaxTolerance,
    decimal ReceiptCostBaseAmount,
    decimal InvoiceNetBaseAmount,
    decimal InvoiceTaxBaseAmount);

public sealed record PurchaseMatchEvaluation(
    PurchaseMatchStatus OverallStatus,
    bool RequiresApproval,
    IReadOnlyList<PurchaseMatchAllocationEvaluation> Allocations,
    decimal ReceiptCostBaseAmount,
    decimal InvoiceNetBaseAmount,
    decimal InvoiceTaxBaseAmount,
    decimal PriceVarianceBaseAmount);
