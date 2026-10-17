namespace OAS.Contracts.Purchasing.SupplierAllocations;

public sealed record SupplierAllocationCenterDto(
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    decimal TotalUnappliedPaymentBaseAmount,
    decimal TotalAvailableCreditBaseAmount,
    IReadOnlyList<SupplierUnappliedPaymentDto> UnappliedPayments,
    IReadOnlyList<SupplierOpenInvoiceDto> OpenInvoices,
    IReadOnlyList<SupplierAvailableCreditDto> AvailableCredits);

public sealed record SupplierUnappliedPaymentDto(
    Guid PaymentVoucherLineId,
    Guid PaymentVoucherId,
    string VoucherNumber,
    DateOnly VoucherDate,
    Guid CurrencyId,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal SourceAmount,
    decimal AppliedAmount,
    decimal UnappliedAmount,
    decimal SourceBaseAmount,
    decimal UnappliedBaseAmount);

public sealed record SupplierOpenInvoiceDto(
    Guid PurchaseInvoiceId,
    string PurchaseInvoiceCode,
    string? SupplierInvoiceCode,
    DateOnly InvoiceDate,
    Guid CurrencyId,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal TotalAmount,
    decimal AppliedPaymentAmount,
    decimal AppliedCreditAmount,
    decimal OutstandingAmount,
    decimal BaseTotalAmount,
    decimal OutstandingBaseAmount);

public sealed record SupplierAvailableCreditDto(
    Guid PurchaseReturnId,
    string ReturnCode,
    DateOnly PostingDate,
    Guid? LinkedPurchaseInvoiceId,
    decimal TotalCreditBaseAmount,
    decimal AppliedCreditBaseAmount,
    decimal AvailableCreditBaseAmount);
