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

public interface ISupplierSettlementFxPostingService
{
    Task<SupplierSettlementFxPostingResult> PostAsync(
        SupplierSettlementFxPostingRequest request,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default);
}
