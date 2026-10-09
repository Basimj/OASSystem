using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record PayPurchaseInvoiceRequest(
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMethod PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    string? ReferenceNumber = null,
    string? Description = null);

public sealed record PayPurchaseInvoiceResultDto(
    Guid PaymentVoucherId,
    string PaymentVoucherNumber,
    Guid PaymentVoucherLineId,
    Guid PaymentAllocationId,
    Guid JournalEntryId,
    decimal PaidAmount,
    decimal OutstandingAmount,
    string PaymentStatus);

public sealed record PurchaseInvoicePaymentSummaryDto(
    Guid PurchaseInvoiceId,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    string PaymentStatus);
