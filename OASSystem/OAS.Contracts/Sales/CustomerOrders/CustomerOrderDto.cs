using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record CustomerOrderDto(
    Guid Id,
    string OrderCode,
    Guid CustomerId,
    string? CustomerCode,
    string? CustomerName,
    Guid? PrescriptionRevisionId,
    DateOnly OrderDate,
    DateOnly? RequiredDate,
    CustomerOrderStatus Status,
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
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string? Notes,
    bool IsActive,
    DateTimeOffset? ConfirmedAtUtc,
    string? ConfirmedBy,
    DateTimeOffset? CancelledAtUtc,
    string? CancelledBy,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy,
    IReadOnlyList<CustomerOrderLineDto> Lines,
    string? PrescriptionCode = null,
    int? PrescriptionRevisionNumber = null)
{
    public SalesPaymentPlan PaymentPlan { get; init; } =
        PaymentTermType == SalesPaymentTermType.Credit ? SalesPaymentPlan.AccountCredit : SalesPaymentPlan.FullNow;
    public SalesDiscountType HeaderDiscountType { get; init; } = SalesDiscountType.None;
    public decimal? HeaderDiscountValue { get; init; }
    public decimal HeaderDiscountAmount { get; init; }
    public string? HeaderDiscountReason { get; init; }
    public DateTimeOffset? HeaderDiscountApprovedAtUtc { get; init; }
    public string? HeaderDiscountApprovedBy { get; init; }
    public bool HeaderDiscountRequiresApproval { get; init; }
}
