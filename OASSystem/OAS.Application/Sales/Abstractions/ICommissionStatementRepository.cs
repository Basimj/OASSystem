using OAS.Application.Abstractions.Persistence;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ICommissionStatementRepository : IRepository<CommissionStatement, Guid>
{
    Task<CommissionStatement?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default);
}
