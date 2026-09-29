namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchaseMatchingDataPort
{
    Task<PurchaseReceiptMatchCandidate?> GetReceiptCandidateAsync(Guid purchaseReceiptLineId, Guid purchaseInvoiceId, CancellationToken cancellationToken = default);
}
