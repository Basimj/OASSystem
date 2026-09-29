using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Generic;

namespace OAS.Infrastructure.Sales.Persistence.Repositories;

public sealed class CustomerOrderAggregateRepository(OasDbContext dbContext)
    : EfRepository<CustomerOrder, Guid>(dbContext), ICustomerOrderAggregateRepository
{
    public Task<CustomerOrder?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<CustomerOrder> query = Set.Include(x => x.Lines);
        if (!tracking)
            query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
