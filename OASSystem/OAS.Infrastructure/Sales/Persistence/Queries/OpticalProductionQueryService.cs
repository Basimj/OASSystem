using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.Production;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Sales.Persistence.Queries;

public sealed class OpticalProductionQueryService(OasDbContext db) : IOpticalProductionQueryService
{
    public async Task<OpticalProductionJobDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Set<OpticalProductionJob>()
            .AsNoTracking()
            .Include(x => x.Materials)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<OpticalProductionJobDto>> ListAsync(
        OpticalProductionStatus? status,
        Guid? salesInvoiceId,
        CancellationToken ct = default)
    {
        var query = db.Set<OpticalProductionJob>().AsNoTracking().Include(x => x.Materials).AsQueryable();
        if (status.HasValue)
        {
            var domainStatus = (OAS.Domain.Sales.Production.OpticalProductionStatus)(byte)status.Value;
            query = query.Where(x => x.Status == domainStatus);
        }
        if (salesInvoiceId.HasValue)
            query = query.Where(x => x.SalesInvoiceId == salesInvoiceId.Value);

        return (await query.OrderByDescending(x => x.JobDate).ThenByDescending(x => x.JobCode).ToListAsync(ct))
            .Select(Map)
            .ToList();
    }

    private static OpticalProductionJobDto Map(OpticalProductionJob x) => new(
        x.Id,
        x.JobCode,
        x.SalesInvoiceId,
        x.SalesInvoiceLineId,
        x.CustomerId,
        x.WarehouseId,
        x.JobDate,
        x.TargetDate,
        (OpticalProductionStatus)(byte)x.Status,
        x.InventoryTransactionId,
        x.MaterialCostBase,
        x.Notes,
        x.RemakeOfJobId,
        x.RemakeNumber,
        x.LastQcResult.HasValue ? (OpticalProductionQcResult?)(byte)x.LastQcResult.Value : null,
        x.QcAttemptCount,
        x.FailedQcCount,
        x.LastQcNotes,
        x.LastQcWasBreakage,
        x.ReleasedAtUtc,
        x.StartedAtUtc,
        x.QcAtUtc,
        x.FailedAtUtc,
        x.CompletedAtUtc,
        Convert.ToBase64String(x.RowVersion),
        x.Materials.Select(m => new OpticalProductionMaterialDto(
            m.Id,
            m.ProductVariantId,
            m.Quantity,
            m.UnitCostSnapshot,
            m.TotalCostSnapshot,
            m.Notes)).ToList());
}
