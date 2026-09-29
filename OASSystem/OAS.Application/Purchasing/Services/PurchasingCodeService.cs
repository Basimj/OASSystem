using OAS.Application.Abstractions.Numbering;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Purchasing.Formatting;

namespace OAS.Application.Purchasing.Services;

public sealed class PurchasingCodeService(ISequenceNumberGenerator sequenceNumberGenerator) : IPurchasingCodeService
{
    public const string PurchaseRequestCodeSequence = "PurchaseRequestCodeSequence";
    public const string PurchaseOrderCodeSequence = "PurchaseOrderCodeSequence";
    public const string PurchaseReceiptCodeSequence = "PurchaseReceiptCodeSequence";
    public const string PurchaseInvoiceCodeSequence = "PurchaseInvoiceCodeSequence";

    public async Task<string> NextPurchaseRequestCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) =>
        PurchaseRequestCodeFormatter.Format(await sequenceNumberGenerator.NextAsync(PurchaseRequestCodeSequence, cancellationToken), documentDate.Year);

    public async Task<string> NextPurchaseOrderCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) =>
        PurchaseOrderCodeFormatter.Format(await sequenceNumberGenerator.NextAsync(PurchaseOrderCodeSequence, cancellationToken), documentDate.Year);

    public async Task<string> NextPurchaseReceiptCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) =>
        PurchaseReceiptCodeFormatter.Format(await sequenceNumberGenerator.NextAsync(PurchaseReceiptCodeSequence, cancellationToken), documentDate.Year);

    public async Task<string> NextPurchaseInvoiceCodeAsync(DateOnly documentDate, CancellationToken cancellationToken = default) =>
        PurchaseInvoiceCodeFormatter.Format(await sequenceNumberGenerator.NextAsync(PurchaseInvoiceCodeSequence, cancellationToken), documentDate.Year);
}
