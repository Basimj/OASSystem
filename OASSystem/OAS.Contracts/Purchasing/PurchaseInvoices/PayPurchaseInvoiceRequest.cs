using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record PayPurchaseInvoiceRequest(
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMethod PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    string? ReferenceNumber = null,
    string? Description = null,
    string? IdempotencyKey = null);

public sealed record PayPurchaseInvoiceResultDto(
    Guid PaymentVoucherId,
    string PaymentVoucherNumber,
    Guid PaymentVoucherLineId,
    Guid PaymentAllocationId,
    Guid JournalEntryId,
    decimal PaidAmount,
    decimal OutstandingAmount,
    string PaymentStatus,
    decimal RealizedExchangeDifferenceBase = 0m,
    Guid? RealizedExchangeJournalEntryId = null);

public sealed record PurchaseInvoicePaymentSettlementDto(
    Guid PaymentAllocationId,
    Guid? PaymentVoucherId,
    Guid? PaymentVoucherLineId,
    string? SourceCurrencyCode,
    decimal SourceAmount,
    decimal? SourceExchangeRate,
    DateOnly? SourceExchangeRateDate,
    ExchangeRateType? SourceExchangeRateType,
    ExchangeRateSource? SourceExchangeRateSource,
    decimal SourceBaseAmount,
    decimal TargetDocumentAmount,
    decimal TargetBaseAmount,
    decimal RealizedExchangeDifferenceBase,
    Guid? RealizedExchangeJournalEntryId,
    DateTimeOffset AllocatedAtUtc);

public sealed record PurchaseInvoicePaymentSummaryDto(
    Guid PurchaseInvoiceId,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    string PaymentStatus,
    decimal AppliedCreditAmount = 0m,
    decimal SettledAmount = 0m,
    decimal BaseTotalAmount = 0m,
    decimal AppliedPaymentBaseAmount = 0m,
    decimal AppliedCreditBaseAmount = 0m,
    decimal OutstandingBaseAmount = 0m,
    decimal RealizedExchangeDifferenceBase = 0m,
    IReadOnlyList<PurchaseInvoicePaymentSettlementDto>? Settlements = null,
    string? BaseCurrencyCode = null);
