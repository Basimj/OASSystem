namespace OAS.Application.Sales.Abstractions;

public sealed record OpticalJobMaterialIssueLine(Guid ProductVariantId, decimal Quantity);
public sealed record OpticalJobInventoryPostingResult(Guid InventoryTransactionId, decimal TotalCostBase);

public interface IOpticalJobInventoryPort
{
    Task<decimal> GetAvailableQuantityAsync(Guid warehouseId, Guid productVariantId, CancellationToken cancellationToken = default);
    Task<OpticalJobInventoryPostingResult> IssueMaterialsAsync(Guid opticalJobId, Guid requestId, string jobCode, Guid warehouseId,
        IReadOnlyCollection<OpticalJobMaterialIssueLine> lines, string createdBy, DateTimeOffset atUtc,
        CancellationToken cancellationToken = default);
    Task<OpticalJobInventoryPostingResult> ScrapAsync(Guid breakageId, string jobCode, Guid warehouseId,
        Guid productVariantId, decimal quantity, string createdBy, DateTimeOffset atUtc,
        CancellationToken cancellationToken = default);
}
