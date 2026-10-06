namespace OAS.Contracts.Sales.Checkout;

public sealed record CheckoutSupplierScheduleRequest(
    Guid CustomerOrderLineId,
    Guid? PreferredSupplierId,
    DateTimeOffset? ScheduledOrderAtUtc,
    DateOnly? RequiredDate);
