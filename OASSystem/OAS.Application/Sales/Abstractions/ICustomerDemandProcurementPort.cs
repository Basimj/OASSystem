namespace OAS.Application.Sales.Abstractions;

public sealed record CustomerDemandShortage(
    Guid CustomerOrderId,
    Guid CustomerOrderLineId,
    Guid WarehouseId,
    Guid ProductVariantId,
    decimal ShortageQuantity,
    DateOnly RequestDate,
    DateOnly? RequiredDate,
    Guid? PreferredSupplierId = null,
    DateTimeOffset? ScheduledOrderAtUtc = null,
    string? Notes = null);

public sealed record CustomerDemandLine(
    Guid PurchaseRequestId,
    Guid PurchaseRequestLineId,
    Guid CustomerOrderId,
    Guid CustomerOrderLineId,
    Guid ProductVariantId,
    decimal RequestedQuantity,
    Guid? PreferredSupplierId,
    DateTimeOffset? ScheduledOrderAtUtc);

public interface ICustomerDemandProcurementPort
{
    Task CreateOrUpdateShortageAsync(
        CustomerDemandShortage shortage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerDemandLine>> GetOpenDemandForOrderAsync(
        Guid customerOrderId,
        CancellationToken cancellationToken = default);

    Task ReassignPreferredSupplierAsync(
        Guid customerOrderLineId,
        Guid productVariantId,
        Guid? preferredSupplierId,
        DateTimeOffset? scheduledOrderAtUtc,
        CancellationToken cancellationToken = default);

    Task CancelUncommittedDemandForOrderAsync(
        Guid customerOrderId,
        string? reason,
        CancellationToken cancellationToken = default);
}
