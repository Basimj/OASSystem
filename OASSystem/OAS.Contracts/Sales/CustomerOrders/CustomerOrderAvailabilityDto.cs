namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record CustomerOrderAvailabilityDto(
    Guid CustomerOrderId,
    IReadOnlyList<CustomerOrderLineAvailabilityDto> Lines);
