using OAS.Domain.Common.Entities;

namespace OAS.Domain.Purchasing.Entities;

public enum ReceiptFollowUpTarget : byte { CustomerOrder = 1, OpticalReplacement = 2 }
public enum ReceiptFollowUpStatus : byte { Pending = 1, Processing = 2, Completed = 3, Failed = 4, DeadLetter = 5 }

/// <summary>Durable pointer, never an alternate inventory or accounting transaction.</summary>
public sealed class PurchaseReceiptFollowUpJob : Entity<Guid>
{
    private PurchaseReceiptFollowUpJob() { }
    public Guid PurchaseReceiptId { get; private set; }
    public Guid? CustomerOrderId { get; private set; }
    public Guid? PurchaseRequestLineId { get; private set; }
    public ReceiptFollowUpTarget TargetKind { get; private set; }
    public ReceiptFollowUpStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public string? LeaseOwner { get; private set; }
    public DateTime? LeaseExpiresAtUtc { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? LastErrorSummary { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PurchaseReceiptFollowUpJob ForCustomer(Guid receiptId, Guid orderId, DateTime now) =>
        New(receiptId, ReceiptFollowUpTarget.CustomerOrder, orderId, null, now);
    public static PurchaseReceiptFollowUpJob ForOptical(Guid receiptId, Guid requestLineId, DateTime now) =>
        New(receiptId, ReceiptFollowUpTarget.OpticalReplacement, null, requestLineId, now);
    private static PurchaseReceiptFollowUpJob New(Guid receiptId, ReceiptFollowUpTarget target, Guid? customerOrderId, Guid? requestLineId, DateTime now)
    {
        if (receiptId == Guid.Empty || (customerOrderId ?? requestLineId).GetValueOrDefault() == Guid.Empty)
            throw new ArgumentException("Receipt and target identities are required.");
        return new PurchaseReceiptFollowUpJob {
            Id = Guid.NewGuid(), PurchaseReceiptId = receiptId, CustomerOrderId = customerOrderId,
            PurchaseRequestLineId = requestLineId, TargetKind = target,
            Status = ReceiptFollowUpStatus.Pending, NextAttemptAtUtc = now, CreatedAtUtc = now
        };
    }
    public void Complete(DateTime now)
    {
        if (Status != ReceiptFollowUpStatus.Processing) throw new InvalidOperationException("Job must be leased before completion.");
        Status = ReceiptFollowUpStatus.Completed; CompletedAtUtc = now;
        LeaseOwner = null; LeaseExpiresAtUtc = null; LastErrorCode = null; LastErrorSummary = null;
    }
    public void Retry(DateTime now)
    {
        if (Status is not (ReceiptFollowUpStatus.Failed or ReceiptFollowUpStatus.DeadLetter))
            throw new InvalidOperationException("Only failed jobs can be retried manually.");
        Status = ReceiptFollowUpStatus.Pending; NextAttemptAtUtc = now;
        LeaseOwner = null; LeaseExpiresAtUtc = null; LastErrorCode = null; LastErrorSummary = null;
    }
}
