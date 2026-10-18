namespace OAS.Contracts.Purchasing.PurchaseReceipts;

public sealed record PurchaseReceiptFollowUpDto(
    Guid Id, Guid PurchaseReceiptId, string ReceiptCode,
    byte TargetKind, Guid? CustomerOrderId, Guid? PurchaseRequestLineId,
    byte Status, int AttemptCount, DateTime NextAttemptAtUtc,
    DateTime? LeaseExpiresAtUtc, string? LastErrorCode, string? LastErrorSummary,
    DateTime CreatedAtUtc, DateTime? CompletedAtUtc);

public sealed record PurchaseReceiptFollowUpPageDto(
    IReadOnlyList<PurchaseReceiptFollowUpDto> Items, long TotalCount, int PageNumber, int PageSize);
