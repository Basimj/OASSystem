using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.OpticalJobs.Services;

public sealed class OpticalJobPurchasingPort(ICustomerDemandProcurementPort procurement) : IOpticalJobPurchasingPort
{
    public async Task<Guid?> CreateReplacementDemandAsync(OpticalJob job, OpticalJobLine line, Guid warehouseId,
        Guid productVariantId, decimal quantity, DateOnly requestDate, string reason, CancellationToken cancellationToken = default)
    {
        await procurement.CreateOrUpdateShortageAsync(new CustomerDemandShortage(
            job.CustomerOrderId,
            line.CustomerOrderLineId,
            warehouseId,
            productVariantId,
            quantity,
            requestDate,
            job.RequiredDate,
            Notes: $"Optical replacement {job.JobCode}: {reason}"), cancellationToken);

        var demand = await procurement.GetOpenDemandForOrderAsync(job.CustomerOrderId, cancellationToken);
        return demand
            .Where(x => x.CustomerOrderLineId == line.CustomerOrderLineId && x.ProductVariantId == productVariantId)
            .OrderByDescending(x => x.RequestedQuantity)
            .Select(x => (Guid?)x.PurchaseRequestLineId)
            .FirstOrDefault();
    }
}
