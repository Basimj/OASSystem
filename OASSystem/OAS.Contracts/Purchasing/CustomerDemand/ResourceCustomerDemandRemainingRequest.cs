namespace OAS.Contracts.Purchasing.CustomerDemand;

public sealed record ResourceCustomerDemandRemainingRequest(
    Guid NewSupplierId,
    DateTimeOffset? ScheduledOrderAtUtc,
    DateOnly? ExpectedDeliveryDate,
    string RowVersion);
