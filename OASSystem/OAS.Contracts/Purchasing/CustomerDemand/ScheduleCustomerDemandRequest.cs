namespace OAS.Contracts.Purchasing.CustomerDemand;

public sealed record ScheduleCustomerDemandRequest(
    DateTimeOffset? ScheduledOrderAtUtc,
    string RowVersion);
