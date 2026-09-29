namespace OAS.Application.Purchasing.Abstractions;

public interface IPurchasingAccountingPort
{
    Task ValidatePostingPeriodAsync(DateOnly postingDate, CancellationToken cancellationToken = default);
    Task ValidateSupplierAccountAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<PurchasingAccountingPostingResult> PostPurchaseReceiptJournalAsync(PurchasingReceiptAccountingContext context, CancellationToken cancellationToken = default);
    Task<PurchasingAccountingPostingResult> PostPurchaseInvoiceJournalAsync(PurchasingInvoiceAccountingContext context, CancellationToken cancellationToken = default);
}
