using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Generic;

namespace OAS.Infrastructure.Sales.Persistence.Repositories;

public sealed class SalesInvoiceAggregateRepository(OasDbContext dbContext)
    : EfRepository<SalesInvoice, Guid>(dbContext), ISalesInvoiceAggregateRepository
{
    public Task<SalesInvoice?> GetAggregateAsync(Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        IQueryable<SalesInvoice> query = Set
            .Include(x => x.Lines)
            .ThenInclude(x => x.PrescriptionSnapshot);

        if (!tracking)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
