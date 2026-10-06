namespace OAS.Contracts.Sales.OrderOperations;

public enum CustomerOrderSupplyStatus : byte
{
    None = 0,
    NotOrdered = 1,
    Scheduled = 2,
    Ordered = 3,
    DueToday = 4,
    Overdue = 5,
    PartiallyReceived = 6,
    Received = 7
}
