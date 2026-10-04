using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.Commissions;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;
namespace OAS.Infrastructure.Sales.Persistence.Queries;
public sealed class CommissionQueryService(OasDbContext db):ICommissionQueryService
{
 public async Task<IReadOnlyList<CommissionRuleDto>> GetRulesAsync(Guid? employeeId,CancellationToken ct=default){
  var q=from r in db.Set<CommissionRule>().AsNoTracking() join e0 in db.Set<Employee>().AsNoTracking() on r.EmployeeId equals e0.Id into eg from e in eg.DefaultIfEmpty() select new{r,e};
  if(employeeId.HasValue) q=q.Where(x=>!x.r.EmployeeId.HasValue||x.r.EmployeeId==employeeId);
  var rows=await q.OrderByDescending(x=>x.r.IsActive).ThenByDescending(x=>x.r.EffectiveFrom).ToListAsync(ct);
  return rows.Select(x=>new CommissionRuleDto(x.r.Id,x.r.Code,x.r.Name,x.r.EmployeeId,x.e==null?null:x.e.FirstName+" "+x.e.LastName,x.r.RatePercent,x.r.EffectiveFrom,x.r.EffectiveTo,x.r.IsActive,Convert.ToBase64String(x.r.RowVersion))).ToList();
 }
 public async Task<IReadOnlyList<CommissionStatementDto>> GetStatementsAsync(Guid? employeeId,DateOnly? fromDate,DateOnly? toDate,CancellationToken ct=default){var q=db.Set<CommissionStatement>().AsNoTracking().Include(x=>x.Entries).AsQueryable();if(employeeId.HasValue)q=q.Where(x=>x.EmployeeId==employeeId);if(fromDate.HasValue)q=q.Where(x=>x.ToDate>=fromDate);if(toDate.HasValue)q=q.Where(x=>x.FromDate<=toDate);var items=await q.OrderByDescending(x=>x.ToDate).ToListAsync(ct);var names=await EmployeeNames(items.Select(x=>x.EmployeeId),ct);return items.Select(x=>Map(x,names.GetValueOrDefault(x.EmployeeId))).ToList();}
 public async Task<CommissionStatementDto?> GetStatementAsync(Guid id,CancellationToken ct=default){var x=await db.Set<CommissionStatement>().AsNoTracking().Include(s=>s.Entries).FirstOrDefaultAsync(s=>s.Id==id,ct);if(x is null)return null;var names=await EmployeeNames([x.EmployeeId],ct);return Map(x,names.GetValueOrDefault(x.EmployeeId));}
 private async Task<Dictionary<Guid,string>> EmployeeNames(IEnumerable<Guid> ids,CancellationToken ct)=>await db.Set<Employee>().AsNoTracking().Where(x=>ids.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.FirstName+" "+x.LastName,ct);
 private static CommissionStatementDto Map(CommissionStatement x,string? name)=>new(x.Id,x.StatementCode,x.EmployeeId,name,x.FromDate,x.ToDate,(CommissionStatementStatus)(byte)x.Status,x.SalesBaseAmount,x.ReturnsBaseAmount,x.CommissionBaseAmount,x.CalculatedAtUtc,x.FinalizedAtUtc,Convert.ToBase64String(x.RowVersion),x.Entries.OrderBy(e=>e.SourceDate).Select(e=>new CommissionEntryDto(e.Id,e.SourceDocumentType,e.SourceDocumentId,e.SourceLineId,e.SalesInvoiceId,e.OriginalSalesInvoiceLineId,e.SalesReturnId,e.SourceDate,e.BaseSalesAmount,e.RatePercent,e.CommissionBaseAmount,e.CommissionRuleId,e.RuleCodeSnapshot,e.RuleNameSnapshot,e.IsReversal)).ToList());
}
