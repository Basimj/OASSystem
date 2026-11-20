using OAS.Contracts.Sales.SalesInvoices;

namespace OAS.Client.Sales.Services;

/// <summary>
/// Durable, exact collection request. A lost HTTP response must never cause
/// a different OperationId (and hence a second receipt) on reload.
/// </summary>
public sealed record SalesInvoicePendingCollection(Guid InvoiceId, CollectSalesInvoicePaymentRequest Request)
{
    public static string StorageKey(Guid invoiceId) => $"oas.sales.invoice.collection.pending.v1:{invoiceId:N}";

    public bool IsValidFor(Guid invoiceId) => InvoiceId == invoiceId && invoiceId != Guid.Empty &&
        Request.OperationId != Guid.Empty && Request.PaymentLines is { Count: > 0 } &&
        Request.PaymentLines.All(line => line.Amount > 0m && line.CurrencyId != Guid.Empty);
}
