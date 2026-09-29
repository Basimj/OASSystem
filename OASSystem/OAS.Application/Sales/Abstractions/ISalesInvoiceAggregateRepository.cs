using OAS.Application.Abstractions.Persistence;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesInvoiceAggregateRepository : IRepository<SalesInvoice, Guid>
{
    Task<SalesInvoice?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default);
}
