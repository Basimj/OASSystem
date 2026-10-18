using Microsoft.EntityFrameworkCore;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Domain.Purchasing.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Persistence;

public sealed class ReceiptFollowUpJobStore(OasDbContext db, TimeProvider clock) : IPurchaseReceiptFollowUpJobs
{
    public async Task EnqueueAsync(Guid receiptId, IReadOnlyCollection<Guid> orderIds,
        IReadOnlyCollection<Guid> requestLineIds, CancellationToken ct)
    {
        if (receiptId == Guid.Empty) throw new ArgumentException("Receipt is required.", nameof(receiptId));
        var orderKeys = orderIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        var requestKeys = requestLineIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        var existing = await db.PurchaseReceiptFollowUpJobs.AsNoTracking()
            .Where(x => x.PurchaseReceiptId == receiptId)
            .Select(x => new { x.CustomerOrderId, x.PurchaseRequestLineId }).ToListAsync(ct);
        var pendingOrders = db.PurchaseReceiptFollowUpJobs.Local
            .Where(x => x.PurchaseReceiptId == receiptId && x.CustomerOrderId.HasValue)
            .Select(x => x.CustomerOrderId!.Value).ToHashSet();
        var pendingLines = db.PurchaseReceiptFollowUpJobs.Local
            .Where(x => x.PurchaseReceiptId == receiptId && x.PurchaseRequestLineId.HasValue)
            .Select(x => x.PurchaseRequestLineId!.Value).ToHashSet();
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var orderId in orderKeys)
            if (!pendingOrders.Contains(orderId) && !existing.Any(x => x.CustomerOrderId == orderId))
                db.PurchaseReceiptFollowUpJobs.Add(PurchaseReceiptFollowUpJob.ForCustomer(receiptId, orderId, now));
        foreach (var lineId in requestKeys)
            if (!pendingLines.Contains(lineId) && !existing.Any(x => x.PurchaseRequestLineId == lineId))
                db.PurchaseReceiptFollowUpJobs.Add(PurchaseReceiptFollowUpJob.ForOptical(receiptId, lineId, now));
        // No SaveChanges: caller MUST write jobs in the SAME receipt-posting transaction.
    }

    public async Task<PurchaseReceiptFollowUpPageDto> ListAsync(Guid? receiptId,
        ReceiptFollowUpStatus? status, int pageNumber, int pageSize, CancellationToken ct)
    {
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, 100);
        var query = db.PurchaseReceiptFollowUpJobs.AsNoTracking();
        if (receiptId.HasValue) query = query.Where(x => x.PurchaseReceiptId == receiptId.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var count = await query.LongCountAsync(ct);
        var rows = await (from j in query
                          join r in db.PurchaseReceipts.AsNoTracking() on j.PurchaseReceiptId equals r.Id
                          orderby j.CreatedAtUtc descending, j.Id
                          select new PurchaseReceiptFollowUpDto(
                              j.Id, j.PurchaseReceiptId, r.ReceiptCode, (byte)j.TargetKind,
                              j.CustomerOrderId, j.PurchaseRequestLineId, (byte)j.Status,
                              j.AttemptCount, j.NextAttemptAtUtc, j.LeaseExpiresAtUtc,
                              j.LastErrorCode, j.LastErrorSummary, j.CreatedAtUtc, j.CompletedAtUtc))
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PurchaseReceiptFollowUpPageDto(rows, count, page, size);
    }

    public async Task<bool> RetryAsync(Guid jobId, CancellationToken ct)
    {
        var job = await db.PurchaseReceiptFollowUpJobs.FirstOrDefaultAsync(x => x.Id == jobId, ct);
        if (job is null) return false;
        if (job.Status is not (ReceiptFollowUpStatus.Failed or ReceiptFollowUpStatus.DeadLetter))
            throw new ConflictException("receipt_followup_not_retryable", "يمكن إعادة محاولة مهام المتابعة المتوقفة أو الفاشلة فقط.");
        job.Retry(clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
