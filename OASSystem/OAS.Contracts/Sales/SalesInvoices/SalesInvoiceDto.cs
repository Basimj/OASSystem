using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.SalesInvoices;

public sealed record SalesInvoiceDto(
    Guid Id,
    string InvoiceCode,
    Guid CustomerId,
    string? CustomerCode,
    string? CustomerName,
    Guid? CustomerOrderId,
    string? CustomerOrderCode,
    Guid? PrescriptionRevisionId,
    DateOnly InvoiceDate,
    DateOnly PostingDate,
    SalesInvoiceStatus Status,
    Guid CurrencyId,
    string CurrencyCodeSnapshot,
    string? CurrencySymbolSnapshot,
    byte CurrencyDecimalPlacesSnapshot,
    decimal ExchangeRate,
    DateOnly ExchangeRateDate,
    ExchangeRateType ExchangeRateType,
    ExchangeRateSource ExchangeRateSource,
    TaxCalculationMode TaxCalculationMode,
    SalesPaymentTermType PaymentTermType,
    int PaymentTermDaysSnapshot,
    DateOnly? DueDate,
    Guid BaseCurrencyId,
    string BaseCurrencyCodeSnapshot,
    byte BaseCurrencyDecimalPlacesSnapshot,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal BaseSubtotal,
    decimal BaseDiscountAmount,
    decimal BaseTaxAmount,
    decimal BaseTotalAmount,
    string? Description,
    Guid? JournalEntryId,
    DateTimeOffset? ConfirmedAtUtc,
    string? ConfirmedBy,
    DateTimeOffset? PostedAtUtc,
    string? PostedBy,
    DateTimeOffset? CancelledAtUtc,
    string? CancelledBy,
    bool IsActive,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy,
    SalesInvoicePaymentSummaryDto PaymentSummary,
    IReadOnlyList<SalesInvoiceLineDto> Lines,
    string? PrescriptionCode = null,
    int? PrescriptionRevisionNumber = null,
    string? JournalEntryNumber = null,
    Guid? SalesEmployeeId = null,
    string? SalesEmployeeName = null)
{
    public SalesPaymentPlan PaymentPlan { get; init; } =
        PaymentTermType == SalesPaymentTermType.Credit ? SalesPaymentPlan.AccountCredit : SalesPaymentPlan.FullNow;
    public SalesDiscountType HeaderDiscountType { get; init; } = SalesDiscountType.None;
    public decimal? HeaderDiscountValue { get; init; }
    public decimal HeaderDiscountAmount { get; init; }
    public decimal BaseHeaderDiscountAmount { get; init; }
    public string? HeaderDiscountReason { get; init; }
    public DateTimeOffset? HeaderDiscountApprovedAtUtc { get; init; }
    public string? HeaderDiscountApprovedBy { get; init; }
    public bool HeaderDiscountRequiresApproval { get; init; }
}
