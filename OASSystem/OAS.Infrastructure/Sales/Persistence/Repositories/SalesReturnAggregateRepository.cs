using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Generic;

namespace OAS.Infrastructure.Sales.Persistence.Repositories;

public sealed class SalesReturnAggregateRepository(OasDbContext dbContext)
    : EfRepository<SalesReturn, Guid>(dbContext), ISalesReturnAggregateRepository
{
    public Task<SalesReturn?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<SalesReturn> query = Set.Include(x => x.Lines);
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
