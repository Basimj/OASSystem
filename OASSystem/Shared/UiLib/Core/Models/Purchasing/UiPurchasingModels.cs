using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Core.Models.Purchasing;

public sealed record UiPurchasingListItem(
    Guid Id,
    string Code,
    string Title,
    string? Subtitle,
    string StatusText,
    string? MetaText = null,
    string IconCssClass = "fa-solid fa-file-lines");

public sealed class UiSupplierCatalogEditorModel
{
    public Guid? Id { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? ProductName { get; set; }
    public string? SupplierProductCode { get; set; }
    public string? SupplierProductName { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public string? PurchaseUnitName { get; set; }
    public decimal UnitConversionFactor { get; set; } = 1m;
    public int? LeadTimeDays { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public bool IsPreferred { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RowVersion { get; set; }
    public List<UiSupplierPriceRowModel> PriceHistory { get; } = [];
    public Guid? NewPriceCurrencyId { get; set; }
    public string? NewPriceCurrencyCode { get; set; }
    public decimal NewPriceUnitPrice { get; set; }
    public DateOnly NewPriceEffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? NewPriceEffectiveTo { get; set; }
    public string? NewPriceNotes { get; set; }
}

public sealed class UiSupplierPriceRowModel
{
    public Guid Id { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsCurrent { get; set; }
    public string? Notes { get; set; }
}

public sealed class UiPurchaseRequestEditorModel
{
    public Guid? Id { get; set; }
    public string RequestCode { get; set; } = "يولد تلقائياً";
    public byte RequestType { get; set; } = 3;
    public byte Status { get; set; } = 1;
    public string StatusText { get; set; } = "مسودة";
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? CustomerOrderId { get; set; }
    public DateOnly RequestDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? RequiredDate { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string? RowVersion { get; set; }
    public List<UiPurchaseRequestLineModel> Lines { get; } = [];
}

public sealed class UiPurchaseRequestLineModel
{
    public Guid? Id { get; set; }
    public int LineSequence { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? ProductName { get; set; }
    public decimal RequestedQuantity { get; set; } = 1m;
    public decimal AllocatedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public DateOnly? RequiredDate { get; set; }
    public Guid? PreferredSupplierId { get; set; }
    public string? PreferredSupplierName { get; set; }
    public string? Notes { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class UiPurchaseOrderEditorModel
{
    public Guid? Id { get; set; }
    public string PurchaseOrderCode { get; set; } = "يولد تلقائياً";
    public byte Status { get; set; } = 1;
    public string StatusText { get; set; } = "مسودة";
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public string? DestinationWarehouseName { get; set; }
    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public Guid? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public DateOnly ExchangeRateDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public byte TaxCalculationMode { get; set; } = 1;
    public int PaymentTermDays { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public string? RowVersion { get; set; }
    public List<UiPurchaseOrderLineModel> Lines { get; } = [];
}

public sealed class UiPurchaseOrderLineModel
{
    public Guid? Id { get; set; }
    public int LineSequence { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? ProductName { get; set; }
    public Guid? SupplierCatalogItemId { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public string? PurchaseUnitName { get; set; }
    public decimal UnitConversionFactor { get; set; } = 1m;
    public decimal OrderedQuantity { get; set; } = 1m;
    public decimal BaseQuantity { get; set; }
    public decimal ReceivedBaseQuantity { get; set; }
    public decimal RemainingBaseQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public string? RowVersion { get; set; }
    public List<UiPurchaseOrderLineSourceModel> Sources { get; } = [];
}

public sealed class UiPurchaseOrderLineSourceModel
{
    public Guid? Id { get; set; }
    public Guid PurchaseRequestLineId { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class UiPurchaseReceiptEditorModel
{
    public Guid? Id { get; set; }
    public string ReceiptCode { get; set; } = "يولد تلقائياً";
    public byte Status { get; set; } = 1;
    public string StatusText { get; set; } = "مسودة";
    public Guid? PurchaseOrderId { get; set; }
    public string? PurchaseOrderCode { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public DateOnly ReceiptDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly PostingDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? SupplierDeliveryCode { get; set; }
    public Guid? InventoryTransactionId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string? Notes { get; set; }
    public string? RowVersion { get; set; }
    public List<UiPurchaseReceiptLineModel> Lines { get; } = [];
}

public sealed class UiPurchaseReceiptLineModel
{
    public Guid? Id { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public int LineSequence { get; set; }
    public Guid ProductVariantId { get; set; }
    public string? ProductName { get; set; }
    public decimal OrderedQuantitySnapshot { get; set; }
    public decimal PreviouslyReceivedQty { get; set; }
    public decimal RemainingReceivableQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal BaseAcceptedQuantity { get; set; }
    public decimal ActualUnitCost { get; set; }
    public decimal TotalAcceptedCost { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? BatchCode { get; set; }
    public string? Notes { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class UiPurchaseInvoiceEditorModel
{
    public Guid? Id { get; set; }
    public string PurchaseInvoiceCode { get; set; } = "يولد تلقائياً";
    public string? SupplierInvoiceCode { get; set; }
    public byte Status { get; set; } = 1;
    public string StatusText { get; set; } = "مسودة";
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly PostingDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public DateOnly ExchangeRateDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public byte TaxCalculationMode { get; set; } = 1;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string? Notes { get; set; }
    public string? RowVersion { get; set; }
    public List<UiPurchaseInvoiceLineModel> Lines { get; } = [];
    public UiPurchaseMatchModel? Match { get; set; }
}

public sealed class UiPurchaseInvoiceLineModel
{
    public Guid? Id { get; set; }
    public int LineSequence { get; set; }
    public Guid? PurchaseOrderLineId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string? RowVersion { get; set; }
}

public sealed class UiPurchaseMatchModel
{
    public string OverallStatus { get; set; } = "لم تتم المطابقة";
    public bool RequiresApproval { get; set; }
    public List<UiPurchaseMatchAllocationModel> Allocations { get; } = [];
    public List<UiPurchaseVarianceModel> Variances { get; } = [];
}

public sealed class UiPurchaseMatchAllocationModel
{
    public Guid Id { get; set; }
    public Guid PurchaseInvoiceLineId { get; set; }
    public Guid PurchaseReceiptLineId { get; set; }
    public decimal MatchedQuantity { get; set; }
    public decimal MatchedNetAmount { get; set; }
    public decimal QuantityVariance { get; set; }
    public decimal PriceVarianceAmount { get; set; }
    public decimal TaxVarianceAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RowVersion { get; set; }
}

public sealed class UiPurchaseVarianceModel
{
    public string Type { get; set; } = string.Empty;
    public Guid PurchaseInvoiceLineId { get; set; }
    public Guid? PurchaseReceiptLineId { get; set; }
    public decimal ExpectedValue { get; set; }
    public decimal ActualValue { get; set; }
    public decimal Variance { get; set; }
    public decimal Tolerance { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}
