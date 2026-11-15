using OAS.Contracts.Sales.SalesInvoices;

namespace OAS.Application.Sales.Abstractions;

/// <summary>Stores a stable result under the ambient command transaction and a SQL Server application lock.</summary>
public interface ISalesInvoiceCollectionIdempotencyStore
{
    Task<CollectSalesInvoicePaymentResultDto?> GetExistingAsync(
        Guid invoiceId, Guid operationId, string fingerprint, CancellationToken cancellationToken);
    Task StoreAsync(Guid invoiceId, Guid operationId, string fingerprint,
        CollectSalesInvoicePaymentResultDto result, CancellationToken cancellationToken);
}
