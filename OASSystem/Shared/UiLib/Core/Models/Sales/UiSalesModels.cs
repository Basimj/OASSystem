namespace OAS.UiLib.Core.Models.Sales;

public sealed record UiSalesDocumentListItem(
    Guid Id,
    string Code,
    string PrimaryText,
    string? SecondaryText,
    string StatusText,
    string StatusCssClass,
    string? AmountText = null,
    string? DateText = null,
    string? MetaText = null);

public sealed class UiPrescriptionFormModel
{
    public Guid? Id { get; set; }
    public string PrescriptionCode { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerDisplay { get; set; }
    public DateOnly? PrescriptionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Status { get; set; } = "Draft";
    public string StatusText { get; set; } = "مسودة";
    public string? PrescribedBy { get; set; }
    public string? ClinicName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public string RowVersion { get; set; } = string.Empty;
    public List<UiPrescriptionRevisionModel> Revisions { get; set; } = [];
}

public sealed class UiPrescriptionRevisionModel
{
    public Guid? Id { get; set; }
    public int RevisionNumber { get; set; }
    public DateOnly? EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? Reason { get; set; }
    public bool IsCurrent { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<UiPrescriptionEyeModel> Eyes { get; set; } = [];
}

public sealed class UiPrescriptionEyeModel
{
    public string Eye { get; set; } = "RightOD";
    public string EyeText { get; set; } = "العين اليمنى OD";
    public decimal? SPH { get; set; }
    public decimal? CYL { get; set; }
    public short? Axis { get; set; }
    public decimal? ADD { get; set; }
    public decimal? Prism { get; set; }
    public string PrismBase { get; set; } = "None";
    public decimal? PD { get; set; }
    public decimal? MonocularPD { get; set; }
    public string? VA { get; set; }
    public decimal? FittingHeight { get; set; }
    public string? Notes { get; set; }
}

public sealed class UiSalesLineModel
{
    public Guid? Id { get; set; }
    public Guid? CustomerOrderLineId { get; set; }
    public Guid? GroupId { get; set; }
    public int LineNumber { get; set; }
    public string LineType { get; set; } = "Frame";
    public string ProductVariantId { get; set; } = string.Empty;
    public string? ProductDisplay { get; set; }
    public string WarehouseId { get; set; } = string.Empty;
    public string? WarehouseDisplay { get; set; }
    public string? ProductCodeSnapshot { get; set; }
    public string? ProductNameSnapshot { get; set; }
    public string? UnitSnapshot { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal BaseUnitPrice { get; set; }
    public decimal ActualUnitPrice { get; set; }
    public string DiscountType { get; set; } = "None";
    public decimal? DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal? UnitCostSnapshot { get; set; }
    public decimal? TotalCostSnapshot { get; set; }
    public string PrescriptionRevisionId { get; set; } = string.Empty;
    public string? PrescriptionRevisionDisplay { get; set; }
    public string PrescriptionEye { get; set; } = string.Empty;
    public bool RequiresProduction { get; set; }
    public string? Notes { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class UiCustomerOrderFormModel
{
    public Guid? Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerDisplay { get; set; }
    public string PrescriptionRevisionId { get; set; } = string.Empty;
    public string? PrescriptionDisplay { get; set; }
    public DateOnly? OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? RequiredDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string StatusText { get; set; } = "مسودة";
    public string CurrencyId { get; set; } = string.Empty;
    public string? CurrencyDisplay { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public byte CurrencyDecimalPlaces { get; set; } = 2;
    public decimal ExchangeRate { get; set; } = 1m;
    public string TaxCalculationMode { get; set; } = "Exclusive";
    public string PaymentTermType { get; set; } = "Immediate";
    public int PaymentTermDays { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<UiSalesLineModel> Lines { get; set; } = [];
}

public sealed class UiSalesInvoiceFormModel
{
    public Guid? Id { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerDisplay { get; set; }
    public string CustomerOrderId { get; set; } = string.Empty;
    public string? CustomerOrderDisplay { get; set; }
    public string PrescriptionRevisionId { get; set; } = string.Empty;
    public string? PrescriptionDisplay { get; set; }
    public DateOnly? InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? PostingDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string StatusText { get; set; } = "مسودة";
    public string CurrencyId { get; set; } = string.Empty;
    public string? CurrencyDisplay { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public byte CurrencyDecimalPlaces { get; set; } = 2;
    public decimal ExchangeRate { get; set; } = 1m;
    public string TaxCalculationMode { get; set; } = "Exclusive";
    public string PaymentTermType { get; set; } = "Immediate";
    public int PaymentTermDays { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string? Description { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<UiSalesLineModel> Lines { get; set; } = [];
    public List<string> PostingIssues { get; set; } = [];
}

public sealed record UiSalesLineSelectionChange(UiSalesLineModel Line, string? Value);
public sealed record UiSalesSelectionChange(string Field, string? Value);
public sealed record UiSalesPriceOverrideRequestModel(decimal OverridePrice, string Reason);
