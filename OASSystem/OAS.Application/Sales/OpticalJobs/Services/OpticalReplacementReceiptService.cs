using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.OpticalJobs.Services;

/// <summary>
/// Resumes optical remakes when a supplier receipt supplies their replacement material.
/// The extra replacement unit is consumed exactly once here (using the breakage id as
/// the inventory/accounting idempotency source); the original sale item is never issued again.
/// </summary>
public sealed class OpticalReplacementReceiptService(
    IRepository<OpticalJobRemake, Guid> remakes,
    IRepository<OpticalJobBreakage, Guid> breakages,
    IReadRepository<OpticalJob, Guid> jobs,
    IOpticalJobInventoryPort inventory,
    IOpticalJobAccountingPort accounting,
    TimeProvider timeProvider) : IOpticalReplacementReceiptService
{
    public async Task ReconcileAsync(Guid warehouseId, IReadOnlyCollection<Guid> purchaseRequestLineIds,
        CancellationToken cancellationToken = default)
    {
        if (warehouseId == Guid.Empty || purchaseRequestLineIds.Count == 0) return;
        var sourceIds = purchaseRequestLineIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (sourceIds.Length == 0) return;

        var rows = await remakes.ListAsync(new Specification<OpticalJobRemake>()
            .Where(x => x.Status == OpticalRemakeStatus.AwaitingMaterials &&
                        x.ReplacementPurchaseRequestLineId.HasValue &&
                        sourceIds.Contains(x.ReplacementPurchaseRequestLineId.Value))
            .Tracking(), cancellationToken);

        foreach (var remake in rows)
        {
            if (!remake.ProductVariantId.HasValue) continue;
            var available = await inventory.GetAvailableQuantityAsync(warehouseId, remake.ProductVariantId.Value, cancellationToken);
            if (available < remake.Quantity) continue;

            if (remake.SourceBreakageId.HasValue)
            {
                var breakage = await breakages.GetForUpdateAsync(remake.SourceBreakageId.Value, cancellationToken);
                if (breakage is not null && !breakage.InventoryTransactionId.HasValue)
                {
                    var job = await jobs.GetByIdAsync(remake.OpticalJobId, cancellationToken);
                    if (job is null) continue;
                    var now = timeProvider.GetUtcNow();
                    var scrap = await inventory.ScrapAsync(breakage.Id, job.JobCode, warehouseId,
                        remake.ProductVariantId.Value, remake.Quantity, "optical-replacement", now, cancellationToken);
                    var journalId = await accounting.PostBreakageAsync(breakage, warehouseId, scrap.TotalCostBase,
                        breakage.RecordedBy, now, cancellationToken);
                    breakage.LinkScrap(scrap.InventoryTransactionId, journalId);
                    breakage.MarkReplacementReceived();
                    breakages.Update(breakage);
                }
            }

            remake.MarkReady();
            remakes.Update(remake);
        }
    }
}
