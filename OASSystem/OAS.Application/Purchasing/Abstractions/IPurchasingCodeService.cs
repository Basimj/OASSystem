namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchasingCodeService
{
    Task<string> NextPurchaseRequestCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default);
    Task<string> NextPurchaseOrderCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default);
    Task<string> NextPurchaseReceiptCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default);
    Task<string> NextPurchaseInvoiceCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default);
    Task<string> NextPurchaseReturnCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default);
}
