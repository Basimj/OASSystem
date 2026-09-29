using OAS.Application.Abstractions.Persistence;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface IPrescriptionAggregateRepository : IRepository<Prescription, Guid>
{
    Task<Prescription?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default);
}
