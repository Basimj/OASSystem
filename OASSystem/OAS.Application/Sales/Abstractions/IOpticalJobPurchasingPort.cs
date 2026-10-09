using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface IOpticalJobPurchasingPort
{
    Task<Guid?> CreateReplacementDemandAsync(OpticalJob job, OpticalJobLine line, Guid warehouseId,
        Guid productVariantId, decimal quantity, DateOnly requestDate, string reason,
        CancellationToken cancellationToken = default);
}
