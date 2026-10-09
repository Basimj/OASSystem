namespace OAS.Application.Sales.Abstractions;

/// <summary>
/// Reconciles supplier receipts that were created to replace broken optical materials.
/// Purchasing invokes this only after its receipt transaction is committed.
/// </summary>
public interface IOpticalReplacementReceiptService
{
    Task ReconcileAsync(
        Guid warehouseId,
        IReadOnlyCollection<Guid> purchaseRequestLineIds,
        CancellationToken cancellationToken = default);
}
