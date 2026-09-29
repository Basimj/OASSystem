using OAS.Application.Abstractions.Persistence;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface ICustomerOrderAggregateRepository : IRepository<CustomerOrder, Guid>
{
    Task<CustomerOrder?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default);
}
