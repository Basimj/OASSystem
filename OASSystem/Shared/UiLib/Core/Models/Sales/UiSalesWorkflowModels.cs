namespace OAS.UiLib.Core.Models.Sales;

public sealed record UiWorkflowMetric(string Key, string Label, int Value, string IconCss, string? CssClass = null);

public sealed class UiSalesCheckoutModel
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? PrescriptionText { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string CurrencyId { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ExistingAdvance { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string PaymentPlan { get; set; } = "FullNow";
    public bool PaymentPlanLocked { get; set; }
    public bool CreditAllowed { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentCreditExposure { get; set; }
    public decimal AvailableCredit { get; set; }
    public int PaymentTermDays { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<UiSalesCheckoutLineModel> Lines { get; set; } = [];
    public List<UiCheckoutPaymentLineModel> PaymentLines { get; set; } = [];
    public List<UiCheckoutShortageModel> Shortages { get; set; } = [];
}

public sealed class UiSalesCheckoutLineModel
{
    public Guid LineId { get; set; }
    public string TypeText { get; set; } = string.Empty;
    public string ProductText { get; set; } = string.Empty;
    public string? EyeText { get; set; }
    public string? OpticalSummary { get; set; }
    public string? WarehouseText { get; set; }
    public string AvailabilityText { get; set; } = string.Empty;
    public string AvailabilityCss { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal FinalAmount { get; set; }
    public bool RequiresProduction { get; set; }
}

public sealed class UiCheckoutPaymentLineModel
{
    public string PaymentMethod { get; set; } = "Cash";
    public string CurrencyId { get; set; } = string.Empty;
    public string? CurrencyDisplay { get; set; }
    public decimal Amount { get; set; }
    public string CashAccountId { get; set; } = string.Empty;
    public string? CashAccountDisplay { get; set; }
    public string BankAccountId { get; set; } = string.Empty;
    public string? BankAccountDisplay { get; set; }
    public string SettlementAccountId { get; set; } = string.Empty;
    public string? SettlementAccountDisplay { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateOnly? ReferenceDate { get; set; }
    public string? Description { get; set; }
}

public sealed class UiCheckoutShortageModel
{
    public Guid CustomerOrderLineId { get; set; }
    public string ProductText { get; set; } = string.Empty;
    public string? EyeText { get; set; }
    public string? OpticalSummary { get; set; }
    public decimal ShortageQuantity { get; set; }
    public string PreferredSupplierId { get; set; } = string.Empty;
    public string? PreferredSupplierDisplay { get; set; }
    public DateTimeOffset? ScheduledOrderAtUtc { get; set; }
    public DateOnly? RequiredDate { get; set; }
    public DateOnly? RequiredDateFrom { get; set; }
    public DateOnly? RequiredDateTo { get; set; }
    public bool? HasBreakage { get; set; }
    public bool? HasRemake { get; set; }
}

public sealed record UiOrderOperationsRowModel(Guid Id,string OrderCode,string OrderDate,string? RequiredDate,string CustomerCode,string CustomerName,string? Mobile,string ItemsSummary,int TotalLines,int AvailableLines,int ShortageLines,string SupplyText,string StatusText,string StatusCss,bool RequiresProduction,string? JobCode,string? Technician,string LastUpdated);
public sealed record UiOrderLineOperationsModel(int LineNumber,string TypeText,string ProductText,string? EyeText,decimal Quantity,string? WarehouseText,string AvailabilityText,string AvailabilityCss,decimal ReservedQuantity,decimal ShortageQuantity,string ProcurementText,string? Supplier,string? ExpectedDelivery,string? OpticalSummary,bool RequiresProduction);
public sealed record UiTimelineItemModel(string Title,string OccurredAt,string? Meta);
public sealed class UiOrderOperationsDetailsModel
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerText { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusKey { get; set; } = string.Empty;
    public string? RequiredDate { get; set; }
    public string? Notes { get; set; }
    public string SupplyText { get; set; } = string.Empty;
    public Guid? JobId { get; set; }
    public string? JobCode { get; set; }
    public List<UiOrderLineOperationsModel> Lines { get; set; } = [];
    public List<UiTimelineItemModel> Timeline { get; set; } = [];
}

public sealed record UiDemandTrackingRowModel(Guid LineId,string RequestCode,string RequestDate,string OrderCode,string CustomerCode,string CustomerName,string? Mobile,string ProductText,string? EyeText,string? OpticalSummary,string? Warehouse,decimal Requested,decimal Allocated,decimal Received,decimal Remaining,string? PreferredSupplier,string? ActualSupplier,string? PurchaseOrderCode,string? ScheduledAt,string? SentAt,string? ExpectedDate,string? LastReceiptDate,string TrackingText,string TrackingCss,int? DelayDays,decimal? UnitPrice,decimal? ActualUnitCost);
public sealed class UiDemandTrackingDetailsModel
{
    public Guid LineId { get; set; }
    public Guid CustomerOrderId { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public string CustomerText { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public Guid ProductVariantId { get; set; }
    public string ProductText { get; set; } = string.Empty;
    public string? OpticalSummary { get; set; }
    public string TrackingText { get; set; } = string.Empty;
    public string TrackingKey { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
    public string PreferredSupplierId { get; set; } = string.Empty;
    public string? PreferredSupplierDisplay { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public decimal Requested { get; set; }
    public decimal Allocated { get; set; }
    public decimal Received { get; set; }
    public decimal Remaining { get; set; }
    public string? Warehouse { get; set; }
    public string? ActualSupplier { get; set; }
    public string? SentAt { get; set; }
    public string? LastReceiptDate { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? ActualUnitCost { get; set; }
    public Guid OrderId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? PurchaseOrderCode { get; set; }
    public string? ExpectedDate { get; set; }
    public DateOnly? ExpectedDeliveryDateInput { get; set; }
    public string ResupplySupplierId { get; set; } = string.Empty;
    public string? ResupplySupplierDisplay { get; set; }
    public DateTimeOffset? ResupplyScheduledAt { get; set; }
}

public sealed record UiOpticalJobRowModel(Guid Id,string JobCode,string? OrderCode,string CustomerText,string? Mobile,string? RequiredDate,string? Frame,string? OD,string? OS,string? Technician,string StatusText,string StatusCss,bool HasOpenBreakage=false,bool HasOpenRemake=false);

public sealed record UiOpticalJobLineModel(
    Guid Id, Guid? ProductVariantId, Guid? GroupId, int LineNumber, string TypeText, string ProductText,
    string? EyeText, decimal Quantity, string? OpticalSummary, string? Notes);

public sealed class UiOpticalQualityCheckItemModel
{
    public Guid Id { get; set; }
    public string CheckCode { get; set; } = string.Empty;
    public string CheckName { get; set; } = string.Empty;
    public string Result { get; set; } = "NotChecked";
    public string? Notes { get; set; }
    public int Sequence { get; set; }
}

public sealed class UiOpticalQualityCheckModel
{
    public Guid Id { get; set; }
    public int AttemptNumber { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? GeneralNotes { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<UiOpticalQualityCheckItemModel> Items { get; set; } = [];
}

public sealed record UiOpticalBreakageModel(Guid Id, Guid OpticalJobLineId, string Status, decimal Quantity, string Reason, string? Eye, DateTimeOffset RecordedAtUtc);
public sealed record UiOpticalRemakeModel(Guid Id, Guid OpticalJobLineId, string Status, decimal Quantity, string Reason, Guid? ReplacementPurchaseRequestLineId, string RowVersion);
public sealed record UiOpticalTimelineModel(string FromStatus, string ToStatus, string? Reason, DateTimeOffset ChangedAtUtc);

public sealed class UiOpticalJobDetailsModel
{
    public Guid Id { get; set; }
    public string JobCode { get; set; } = string.Empty;
    public string? OrderCode { get; set; }
    public string CustomerText { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? RequiredDate { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusKey { get; set; } = string.Empty;
    public string? Technician { get; set; }
    public string TechnicianId { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<UiOpticalJobLineModel> Lines { get; set; } = [];
    public UiOpticalQualityCheckModel? LatestQualityCheck { get; set; }
    public List<UiOpticalBreakageModel> Breakages { get; set; } = [];
    public List<UiOpticalRemakeModel> Remakes { get; set; } = [];
    public List<UiOpticalTimelineModel> Timeline { get; set; } = [];

    public string BreakageLineId { get; set; } = string.Empty;
    public decimal BreakageQuantity { get; set; } = 1m;
    public string BreakageReasonCode { get; set; } = "LAB_BREAKAGE";
    public string? BreakageReasonText { get; set; }
    public bool BreakageRequiresReplacement { get; set; } = true;
    public string BreakageWarehouseId { get; set; } = string.Empty;
    public string? BreakageWarehouseDisplay { get; set; }

    public string MaterialLineId { get; set; } = string.Empty;
    public decimal MaterialQuantity { get; set; } = 1m;
    public string MaterialWarehouseId { get; set; } = string.Empty;
    public string? MaterialWarehouseDisplay { get; set; }

    public string RemakeLineId { get; set; } = string.Empty;
    public decimal RemakeQuantity { get; set; } = 1m;
    public string? RemakeReason { get; set; }
    public string RemakeWarehouseId { get; set; } = string.Empty;
    public string? RemakeWarehouseDisplay { get; set; }

    public string QcFailureAction { get; set; } = "Rework";
    public string? QcFailureReason { get; set; }
    public string QcRemakeLineId { get; set; } = string.Empty;
    public decimal QcRemakeQuantity { get; set; } = 1m;
}

public sealed class UiDeliverySettlementModel
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerText { get; set; } = string.Empty;
    public string? InvoiceCode { get; set; }
    public string CurrencyId { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal AppliedAdvances { get; set; }
    public decimal Outstanding { get; set; }
    public string PaymentPlan { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
    public List<UiCheckoutPaymentLineModel> PaymentLines { get; set; } = [];
}

public sealed class UiSalesCheckoutSelectionModel
{
    public string OrderId { get; set; } = string.Empty;
    public string? OrderDisplay { get; set; }
}

public sealed class UiOrderOperationsFilterModel
{
    public string? Search { get; set; }
    public DateOnly? OrderDateFrom { get; set; }
    public DateOnly? OrderDateTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public string? WarehouseDisplay { get; set; }
    public string RequiresProduction { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public DateOnly? RequiredDate { get; set; }
    public string TechnicianId { get; set; } = string.Empty;
    public string? TechnicianDisplay { get; set; }
    public bool OverdueOnly { get; set; }
}

public sealed class UiDemandTrackingFilterModel
{
    public string? Search { get; set; }
    public DateOnly? RequestDateFrom { get; set; }
    public DateOnly? RequestDateTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public string? SupplierDisplay { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public string? WarehouseDisplay { get; set; }
    public string CustomerOrderId { get; set; } = string.Empty;
    public string? CustomerOrderDisplay { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerDisplay { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public bool OverdueOnly { get; set; }
}

public sealed class UiOpticalJobFilterModel
{
    public string? Search { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TechnicianId { get; set; } = string.Empty;
    public string? TechnicianDisplay { get; set; }
    public DateOnly? RequiredDate { get; set; }
    public DateOnly? RequiredDateFrom { get; set; }
    public DateOnly? RequiredDateTo { get; set; }
    public bool? HasBreakage { get; set; }
    public bool? HasRemake { get; set; }
}

public sealed record UiDemandActionModel(string Action, Guid LineId, string SupplierId, DateTimeOffset? ScheduledAt, DateOnly? ExpectedDate, string RowVersion);

public sealed record UiSupplierHistoryRowModel(string OrderCode,string OrderDate,string ProductText,decimal Ordered,decimal Received,string? ExpectedDate,string? ReceiptCode,string? ReceiptDate,decimal? UnitPrice,decimal? ActualUnitCost);
