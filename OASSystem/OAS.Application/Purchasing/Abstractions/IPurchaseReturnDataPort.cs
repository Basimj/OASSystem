namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchaseReturnDataPort
{
    Task<bool> HasPostedInvoiceAllocationAsync(Guid purchaseReceiptLineId, CancellationToken cancellationToken = default);
}
