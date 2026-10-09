using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface IOpticalJobSalesPort
{
    Task MarkReadyForDeliveryAsync(OpticalJob job, CancellationToken cancellationToken = default);
    Task EnsureDeliveryEligibleAsync(OpticalJob job, CancellationToken cancellationToken = default);
    Task MarkDeliveredAsync(OpticalJob job, CancellationToken cancellationToken = default);
}
