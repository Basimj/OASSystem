using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesInvoiceAccountingPostingService
{
    Task<Guid> PostAsync(SalesInvoice invoice, Guid postedBy, DateTime postedAtUtc, CancellationToken cancellationToken = default);
}
