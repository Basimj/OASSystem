using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public sealed record SupplierCreditSnapshot(
    Guid PurchaseReturnId,
    Guid SupplierId,
    decimal TotalCreditBaseAmount,
    decimal AppliedCreditBaseAmount,
    decimal AvailableCreditBaseAmount,
    IReadOnlyList<SupplierCreditApplication> Applications);

public interface ISupplierCreditSettlementService
{
    Task<SupplierCreditApplication?> AutoApplyLinkedReturnAsync(
        PurchaseReturn purchaseReturn,
        DateTime appliedAtUtc,
        string appliedBy,
        CancellationToken cancellationToken = default);

    Task<SupplierCreditApplication> ApplyAsync(
        Guid purchaseReturnId,
        Guid purchaseInvoiceId,
        decimal? appliedBaseAmount,
        string idempotencyKey,
        DateTime appliedAtUtc,
        string appliedBy,
        CancellationToken cancellationToken = default);

    Task<SupplierCreditApplication> ReverseAsync(
        Guid purchaseReturnId,
        Guid applicationId,
        string rowVersion,
        string reason,
        DateTime reversedAtUtc,
        string reversedBy,
        CancellationToken cancellationToken = default);

    Task<SupplierCreditSnapshot> GetSnapshotAsync(
        Guid purchaseReturnId,
        CancellationToken cancellationToken = default);
}
