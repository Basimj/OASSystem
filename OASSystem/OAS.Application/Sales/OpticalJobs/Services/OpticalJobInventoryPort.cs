using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Sales.OpticalJobs.Services;

public sealed class OpticalJobInventoryPort(
    IInventoryBalanceRepository balances,
    IInventoryTransactionRepository transactions,
    IRepository<InventoryTransactionLine, Guid> transactionLines,
    IInventoryPostingService posting,
    ISequenceNumberGenerator sequences) : IOpticalJobInventoryPort
{
    public async Task<decimal> GetAvailableQuantityAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default)
        => (await balances.GetByWarehouseAndVariantAsync(warehouseId, productVariantId, cancellationToken))?.AvailableQuantity ?? 0m;

    public async Task<OpticalJobInventoryPostingResult> IssueMaterialsAsync(Guid opticalJobId, Guid requestId, string jobCode, Guid warehouseId,
        IReadOnlyCollection<OpticalJobMaterialIssueLine> lines, string createdBy, DateTimeOffset atUtc, CancellationToken cancellationToken = default)
    {
        if (requestId == Guid.Empty) throw new ConflictException("optical_job_material_request_id_required", "معرف طلب صرف المواد مطلوب.");
        if (lines.Count == 0) throw new ConflictException("optical_job_materials_required", "يجب تحديد مواد إضافية واحدة على الأقل للصرف.");
        var existing = (await transactions.ListAsync(new Specification<InventoryTransaction>().Where(x =>
            x.TransactionType == InventoryTransactionType.ProductionIssue && x.ReferenceType == "OpticalJobMaterialIssue" && x.ReferenceId == requestId), cancellationToken)).FirstOrDefault();
        if (existing is not null)
        {
            var oldLines = await transactions.GetLinesAsync(existing.Id, cancellationToken);
            return new(existing.Id, oldLines.Sum(x => x.TotalCost));
        }

        var seq = await sequences.NextAsync("InventoryTransaction", cancellationToken);
        var tx = new InventoryTransaction($"TXN-{atUtc.Year:0000}-{seq:000000}", InventoryTransactionType.ProductionIssue, atUtc,
            sourceWarehouseId: warehouseId, referenceType: "OpticalJobMaterialIssue", referenceId: requestId,
            reason: $"Optical job additional materials {jobCode}");
        await transactions.AddAsync(tx, cancellationToken);
        decimal total = 0m;
        foreach (var material in lines.GroupBy(x => x.ProductVariantId).Select(g => new OpticalJobMaterialIssueLine(g.Key, g.Sum(x => x.Quantity))))
        {
            if (material.ProductVariantId == Guid.Empty || material.Quantity <= 0m)
                throw new ConflictException("optical_job_material_invalid", "بيانات مادة الإنتاج غير صالحة.");
            var cost = await posting.GetAvailableOutboundUnitCostAsync(warehouseId, material.ProductVariantId, material.Quantity, cancellationToken);
            var line = new InventoryTransactionLine(tx.Id, material.ProductVariantId, material.Quantity, cost, $"Optical job {jobCode}");
            await transactionLines.AddAsync(line, cancellationToken);
            await posting.PostMovementAsync(warehouseId, material.ProductVariantId, InventoryMovementType.Out, material.Quantity, cost,
                tx.Id, line.Id, atUtc, createdBy, cancellationToken);
            total += line.TotalCost;
        }
        tx.Post(atUtc, createdBy);
        return new(tx.Id, total);
    }

    public async Task<OpticalJobInventoryPostingResult> ScrapAsync(Guid breakageId, string jobCode, Guid warehouseId,
        Guid productVariantId, decimal quantity, string createdBy, DateTimeOffset atUtc, CancellationToken cancellationToken = default)
    {
        var existing = (await transactions.ListAsync(new Specification<InventoryTransaction>().Where(x =>
            x.TransactionType == InventoryTransactionType.Scrap && x.ReferenceType == "OpticalJobBreakage" && x.ReferenceId == breakageId), cancellationToken)).FirstOrDefault();
        if (existing is not null)
        {
            var oldLines = await transactions.GetLinesAsync(existing.Id, cancellationToken);
            return new(existing.Id, oldLines.Sum(x => x.TotalCost));
        }
        if (warehouseId == Guid.Empty || productVariantId == Guid.Empty || quantity <= 0m)
            throw new ConflictException("optical_breakage_inventory_invalid", "بيانات المخزون للكسر غير صالحة.");

        var seq = await sequences.NextAsync("InventoryTransaction", cancellationToken);
        var tx = new InventoryTransaction($"TXN-{atUtc.Year:0000}-{seq:000000}", InventoryTransactionType.Scrap, atUtc,
            sourceWarehouseId: warehouseId, referenceType: "OpticalJobBreakage", referenceId: breakageId,
            reason: $"Optical breakage {jobCode}");
        await transactions.AddAsync(tx, cancellationToken);
        var cost = await posting.GetAvailableOutboundUnitCostAsync(warehouseId, productVariantId, quantity, cancellationToken);
        var line = new InventoryTransactionLine(tx.Id, productVariantId, quantity, cost, $"Breakage {jobCode}");
        await transactionLines.AddAsync(line, cancellationToken);
        await posting.PostMovementAsync(warehouseId, productVariantId, InventoryMovementType.Out, quantity, cost,
            tx.Id, line.Id, atUtc, createdBy, cancellationToken);
        tx.Post(atUtc, createdBy);
        return new(tx.Id, line.TotalCost);
    }
}
