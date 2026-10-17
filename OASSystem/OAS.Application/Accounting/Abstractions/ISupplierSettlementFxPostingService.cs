namespace OAS.Application.Accounting.Abstractions;

public sealed record SupplierSettlementFxPostingRequest(
    Guid PaymentAllocationId,
    Guid SupplierId,
    Guid SupplierAccountId,
    DateOnly PostingDate,
    decimal SourceBaseAmount,
    decimal TargetBaseAmount,
    string Description);

public sealed record SupplierSettlementFxPostingResult(
    Guid? JournalEntryId,
    decimal RealizedExchangeDifferenceBase);

public sealed record SupplierSettlementFxReversalRequest(
    Guid PaymentAllocationId,
    DateOnly PostingDate,
    string Description);

public sealed record SupplierSettlementFxReversalResult(
    Guid? OriginalJournalEntryId,
    Guid? ReversalJournalEntryId);

public interface ISupplierSettlementFxPostingService
{
    Task<SupplierSettlementFxPostingResult> PostAsync(
        SupplierSettlementFxPostingRequest request,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default);

    Task<SupplierSettlementFxReversalResult> ReverseAsync(
        SupplierSettlementFxReversalRequest request,
        Guid reversedBy,
        DateTime reversedAtUtc,
        CancellationToken cancellationToken = default);
}
