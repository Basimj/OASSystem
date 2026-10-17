namespace OAS.Application.Sales.Abstractions;

public interface ISalesPaymentAllocationConcurrencyPort
{
    Task LockInvoiceAsync(
        Guid salesInvoiceId,
        CancellationToken cancellationToken = default);

    Task LockReceiptLineAndInvoiceAsync(
        Guid receiptVoucherLineId,
        Guid salesInvoiceId,
        CancellationToken cancellationToken = default);
}
