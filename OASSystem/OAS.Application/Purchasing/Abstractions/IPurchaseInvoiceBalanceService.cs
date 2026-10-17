using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public sealed record PurchaseInvoiceBalanceSnapshot(
    Guid PurchaseInvoiceId,
    decimal TotalAmount,
    decimal BaseTotalAmount,
    decimal AppliedPaymentBaseAmount,
    decimal AppliedCreditBaseAmount,
    decimal AppliedCreditInvoiceCurrencyAmount,
    decimal OutstandingBaseAmount,
    decimal OutstandingAmount,
    decimal PaidAmount,
    decimal SettledAmount,
    decimal AppliedPaymentInvoiceCurrencyAmount = 0m,
    decimal RealizedExchangeDifferenceBase = 0m);

public interface IPurchaseInvoiceBalanceService
{
    Task<PurchaseInvoiceBalanceSnapshot> GetAsync(
        PurchaseInvoice invoice,
        Guid? excludingPaymentAllocationId = null,
        CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceBalanceSnapshot> GetAsync(
        Guid purchaseInvoiceId,
        Guid? excludingPaymentAllocationId = null,
        CancellationToken cancellationToken = default);
}
