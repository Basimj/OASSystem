using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public interface IOpticalJobAccountingPort
{
    Task<Guid> PostBreakageAsync(OpticalJobBreakage breakage, Guid warehouseId, decimal totalCostBase,
        Guid postedBy, DateTimeOffset postedAtUtc, CancellationToken cancellationToken = default);
}
