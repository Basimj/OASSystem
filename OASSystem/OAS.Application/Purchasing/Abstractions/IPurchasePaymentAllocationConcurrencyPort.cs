namespace OAS.Application.Purchasing.Abstractions;

/// <summary>
/// Serializes supplier-payment allocation decisions against the SQL rows that own
/// the source amount and the purchase-invoice outstanding amount. Implementations
/// must execute inside the ambient command transaction.
/// </summary>
public interface IPurchasePaymentAllocationConcurrencyPort
{
    Task LockInvoiceAsync(
        Guid purchaseInvoiceId,
        CancellationToken cancellationToken = default);

    Task LockPaymentLineAndInvoiceAsync(
        Guid paymentVoucherLineId,
        Guid purchaseInvoiceId,
        CancellationToken cancellationToken = default);
}
