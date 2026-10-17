namespace OAS.Application.Accounting.Abstractions;

public sealed record CustomerSettlementFxPostingRequest(
    Guid PaymentAllocationId,
    Guid CustomerId,
    Guid CustomerAccountId,
    DateOnly PostingDate,
    decimal SourceBaseAmount,
    decimal TargetBaseAmount,
    string Description);

public sealed record CustomerSettlementFxPostingResult(
    Guid? JournalEntryId,
    decimal RealizedExchangeDifferenceBase);

public sealed record CustomerSettlementFxReversalRequest(
    Guid PaymentAllocationId,
    DateOnly PostingDate,
    string Description);

public sealed record CustomerSettlementFxReversalResult(
    Guid? OriginalJournalEntryId,
    Guid? ReversalJournalEntryId);

public interface ICustomerSettlementFxPostingService
{
    Task<CustomerSettlementFxPostingResult> PostAsync(
        CustomerSettlementFxPostingRequest request,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default);

    Task<CustomerSettlementFxReversalResult> ReverseAsync(
        CustomerSettlementFxReversalRequest request,
        Guid reversedBy,
        DateTime reversedAtUtc,
        CancellationToken cancellationToken = default);
}
