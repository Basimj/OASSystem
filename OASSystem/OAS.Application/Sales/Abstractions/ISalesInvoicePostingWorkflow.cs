using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public sealed record SalesInvoicePostingOutcome(
    Guid JournalEntryId,
    IReadOnlyList<Guid> InventoryTransactionIds);

public interface ISalesInvoicePostingWorkflow
{
    Task<SalesInvoicePostingOutcome> PostAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken = default);
}
