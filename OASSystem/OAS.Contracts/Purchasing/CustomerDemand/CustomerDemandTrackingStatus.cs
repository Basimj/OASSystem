namespace OAS.Contracts.Purchasing.CustomerDemand;

public enum CustomerDemandTrackingStatus : byte
{
    New = 1,
    AwaitingSupplier = 2,
    Scheduled = 3,
    Ordered = 4,
    DueToday = 5,
    Overdue = 6,
    PartiallyReceived = 7,
    Received = 8
}
