namespace OAS.Contracts.Purchasing.CustomerDemand;

public sealed record CustomerDemandTrackingSummaryDto(
    int New,
    int AwaitingSupplier,
    int Scheduled,
    int Ordered,
    int DueToday,
    int Overdue,
    int PartiallyReceived,
    int Received,
    int Total);
