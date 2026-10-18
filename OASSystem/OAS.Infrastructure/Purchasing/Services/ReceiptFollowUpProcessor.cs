using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Services;

/// <summary>Leases work atomically; every Sales/Optical effect and job completion share ONE transaction.</summary>
public sealed class ReceiptFollowUpProcessor(
    OasDbContext db, IUnitOfWork uow,
    ICustomerOrderFulfillmentService customerOrders,
    IOpticalReplacementReceiptService opticalReceipts,
    TimeProvider clock, ILogger<ReceiptFollowUpProcessor> logger)
{
    private const int MaximumAttempts = 8;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(10);

    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var candidates = await db.PurchaseReceiptFollowUpJobs.AsNoTracking()
            .Where(x => (x.Status == ReceiptFollowUpStatus.Pending ||
                         x.Status == ReceiptFollowUpStatus.Failed ||
                         (x.Status == ReceiptFollowUpStatus.Processing && x.LeaseExpiresAtUtc < now)) &&
                        x.NextAttemptAtUtc <= now)
            .OrderBy(x => x.NextAttemptAtUtc).ThenBy(x => x.TargetKind).ThenBy(x => x.Id)
            .Select(x => x.Id).Take(20).ToListAsync(ct);
        foreach (var id in candidates)
        {
            var owner = Guid.NewGuid().ToString("N");
            // Compare-and-swap update; only one worker can claim this row.
            var claimed = await db.PurchaseReceiptFollowUpJobs
                .Where(x => x.Id == id &&
                    (x.Status == ReceiptFollowUpStatus.Pending || x.Status == ReceiptFollowUpStatus.Failed ||
                     (x.Status == ReceiptFollowUpStatus.Processing && x.LeaseExpiresAtUtc < now)) &&
                    x.NextAttemptAtUtc <= now)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, ReceiptFollowUpStatus.Processing)
                    .SetProperty(x => x.LeaseOwner, owner)
                    .SetProperty(x => x.LeaseExpiresAtUtc, now.Add(LeaseDuration))
                    .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), ct);
            if (claimed == 0) continue;
            try
            {
                await uow.ExecuteInTransactionAsync(async tx =>
                {
                    // Row lock is held until Sales/Optical reconciliation and job completion commit.
                    // If lease expires mid-work, another worker cannot process simultaneously.
                    var locked = await db.PurchaseReceiptFollowUpJobs
                        .FromSqlInterpolated($"SELECT * FROM [dbo].[tbl_PurchasingIntegrationJobs] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {id}")
                        .ToListAsync(tx);
                    var job = locked.Single();
                    if (job.LeaseOwner != owner || job.Status != ReceiptFollowUpStatus.Processing)
                        return false;
                    var receipt = await db.PurchaseReceipts.AsNoTracking()
                        .SingleOrDefaultAsync(x => x.Id == job.PurchaseReceiptId, tx)
                        ?? throw new InvalidOperationException("Source receipt not found.");
                    if (receipt.Status != OAS.Domain.Purchasing.Enums.PurchaseReceiptStatus.Posted ||
                        !receipt.InventoryTransactionId.HasValue || !receipt.JournalEntryId.HasValue)
                        throw new InvalidOperationException("Source receipt must be fully posted before follow-up.");
                    if (job.TargetKind == ReceiptFollowUpTarget.CustomerOrder)
                        await customerOrders.ReconcileOrdersAsync([job.CustomerOrderId!.Value], tx);
                    else if (job.TargetKind == ReceiptFollowUpTarget.OpticalReplacement)
                        await opticalReceipts.ReconcileAsync(receipt.WarehouseId, [job.PurchaseRequestLineId!.Value], tx);
                    else throw new InvalidOperationException("Unsupported target kind.");
                    job.Complete(clock.GetUtcNow().UtcDateTime);
                    return true;
                }, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                // Transaction was rolled back; clear tracked changes before the independent failure update.
                db.ChangeTracker.Clear();
                var attempts = await db.PurchaseReceiptFollowUpJobs.AsNoTracking()
                    .Where(x => x.Id == id && x.LeaseOwner == owner).Select(x => x.AttemptCount)
                    .FirstOrDefaultAsync(ct);
                var dead = attempts >= MaximumAttempts;
                var delaySeconds = Math.Min(3600, 15 * (int)Math.Pow(2, Math.Min(attempts, 8)));
                var failedAt = clock.GetUtcNow().UtcDateTime;
                var retryAt = failedAt.AddSeconds(delaySeconds);
                var errorSummary = ex.GetType().Name;
                await db.PurchaseReceiptFollowUpJobs.Where(x => x.Id == id && x.LeaseOwner == owner &&
                        x.Status == ReceiptFollowUpStatus.Processing)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Status, dead ? ReceiptFollowUpStatus.DeadLetter : ReceiptFollowUpStatus.Failed)
                        .SetProperty(x => x.NextAttemptAtUtc, retryAt)
                        .SetProperty(x => x.LeaseOwner, (string?)null)
                        .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                        .SetProperty(x => x.LastErrorCode, "follow_up_failed")
                        .SetProperty(x => x.LastErrorSummary, errorSummary), ct);
                logger.LogWarning(ex, "Purchase receipt integration follow-up {JobId} failed at attempt {Attempt}", id, attempts);
            }
            return true;
        }
        return false;
    }
}
