namespace OAS.Domain.Sales.Enums;

public enum CustomerOrderStatus : byte
{
    Draft = 1,
    Confirmed = 2,
    AwaitingStock = 3,
    PartiallyAvailable = 4,
    ReadyForProduction = 5,
    InProduction = 6,
    ReadyForDelivery = 7,
    Completed = 8,
    Cancelled = 9
}
