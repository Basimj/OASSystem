using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Repositories.Generic;
namespace OAS.Infrastructure.Sales.Persistence.Repositories;
public sealed class CommissionStatementRepository(OasDbContext db):EfRepository<CommissionStatement,Guid>(db),ICommissionStatementRepository
{
 public Task<CommissionStatement?> GetAggregateAsync(Guid id,bool tracking,CancellationToken ct=default){IQueryable<CommissionStatement> q=Set.Include(x=>x.Entries);if(!tracking)q=q.AsNoTracking();return q.FirstOrDefaultAsync(x=>x.Id==id,ct);}
}
