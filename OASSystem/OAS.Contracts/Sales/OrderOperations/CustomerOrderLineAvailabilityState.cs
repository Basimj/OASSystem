namespace OAS.Contracts.Sales.OrderOperations;

public enum CustomerOrderLineAvailabilityState : byte
{
    Available = 1,
    Reserved = 2,
    Shortage = 3
}
