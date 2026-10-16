using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Production.Services;

public sealed class OpticalProductionInventoryService(
    IInventoryTransactionRepository transactions,
    IRepository<InventoryTransactionLine, Guid> lines,
    IInventoryPostingService posting,
    IReadRepository<Warehouse, Guid> warehouses,
    ISequenceNumberGenerator sequences) : IOpticalProductionInventoryService
{
    public async Task<Guid> IssueMaterialsAsync(OpticalProductionJob job, string user, DateTimeOffset at, CancellationToken ct = default)
    {
        var warehouse = await warehouses.GetByIdAsync(job.WarehouseId, ct)
            ?? throw new ConflictException("optical_production_warehouse_missing", "مخزن الإنتاج غير موجود.");
        if (!warehouse.IsActive)
            throw new ConflictException("optical_production_warehouse_inactive", "مخزن الإنتاج غير فعال.");
        if (!warehouse.AllowsSalesAllocation)
            throw new ConflictException("optical_production_warehouse_quarantined", "لا يمكن صرف مواد إنتاج من مخزن حجر/غير مخصص للبيع.");

        var seq = await sequences.NextAsync("InventoryTransaction", ct);
        var tx = new InventoryTransaction(
            $"TXN-{job.JobDate.Year:0000}-{seq:000000}",
            InventoryTransactionType.ProductionIssue,
            at,
            sourceWarehouseId: job.WarehouseId,
            referenceType: "OpticalProduction",
            referenceId: job.Id,
            reason: $"Optical production {job.JobCode}");
        await transactions.AddAsync(tx, ct);

        foreach (var material in job.Materials)
        {
            var cost = await posting.GetAvailableOutboundUnitCostAsync(job.WarehouseId, material.ProductVariantId, material.Quantity, ct);
            material.SetIssueCost(cost);
            var line = new InventoryTransactionLine(tx.Id, material.ProductVariantId, material.Quantity, cost, $"Production {job.JobCode}");
            await lines.AddAsync(line, ct);
            await posting.PostMovementAsync(
                job.WarehouseId, material.ProductVariantId, InventoryMovementType.Out, material.Quantity, cost,
                tx.Id, line.Id, at, user, ct);
        }

        tx.Post(at, user);
        job.SetMaterialIssue(tx.Id);
        return tx.Id;
    }
}
