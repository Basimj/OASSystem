namespace OAS.Contracts.Accounting.PaymentAllocations;

public sealed record ReversePaymentAllocationRequest(
    DateOnly ReversalDate,
    string Reason,
    string RowVersion);

public sealed record ReversePaymentAllocationResultDto(
    Guid PaymentAllocationId,
    Guid? OriginalFxJournalEntryId,
    Guid? ReversalFxJournalEntryId,
    bool AlreadyReversed);
