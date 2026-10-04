using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Infrastructure.Persistence;
namespace OAS.Infrastructure.Sales.Persistence.Queries;
public sealed class CommissionSourceQueryService(OasDbContext db):ICommissionSourceQueryService
{
 public async Task<IReadOnlyList<CommissionSourceLine>> GetSourcesAsync(Guid employeeId,DateOnly fromDate,DateOnly toDate,CancellationToken ct=default)
 {
  var invoices=await (from i in db.Set<SalesInvoice>().AsNoTracking() join l in db.Set<SalesInvoiceLine>().AsNoTracking() on i.Id equals l.SalesInvoiceId
   where i.Status==SalesInvoiceStatus.Posted && i.SalesEmployeeId==employeeId && i.PostingDate>=fromDate && i.PostingDate<=toDate && l.IsActive
   select new CommissionSourceLine("SalesInvoice",i.Id,l.Id,i.Id,l.Id,null,i.PostingDate,i.PostingDate,l.BaseNetAmount,false)).ToListAsync(ct);
  var returns=await (from r in db.Set<SalesReturn>().AsNoTracking() join rl in db.Set<SalesReturnLine>().AsNoTracking() on r.Id equals rl.SalesReturnId
   join i in db.Set<SalesInvoice>().AsNoTracking() on r.SalesInvoiceId equals i.Id
   where r.Status==OAS.Domain.Sales.Enums.SalesReturnStatus.Posted && i.SalesEmployeeId==employeeId && r.PostingDate>=fromDate && r.PostingDate<=toDate && rl.IsActive
   select new CommissionSourceLine("SalesReturn",r.Id,rl.Id,i.Id,rl.SalesInvoiceLineId,r.Id,r.PostingDate,i.PostingDate,rl.BaseNetAmount,true)).ToListAsync(ct);
  return invoices.Concat(returns).OrderBy(x=>x.SourceDate).ThenBy(x=>x.SourceDocumentType).ToList();
 }
}
