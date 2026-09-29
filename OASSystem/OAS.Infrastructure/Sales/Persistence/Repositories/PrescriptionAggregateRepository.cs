using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Generic;

namespace OAS.Infrastructure.Sales.Persistence.Repositories;

public sealed class PrescriptionAggregateRepository(OasDbContext dbContext)
    : EfRepository<Prescription, Guid>(dbContext), IPrescriptionAggregateRepository
{
    public Task<Prescription?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<Prescription> query = Set
            .Include(x => x.Revisions)
            .ThenInclude(x => x.EyeDetails);

        if (!tracking)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
