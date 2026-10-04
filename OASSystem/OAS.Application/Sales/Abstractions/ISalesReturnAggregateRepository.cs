using OAS.Application.Abstractions.Persistence;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesReturnAggregateRepository : IRepository<SalesReturn, Guid>
{
    Task<SalesReturn?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default);
}
