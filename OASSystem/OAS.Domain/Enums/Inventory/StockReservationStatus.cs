namespace OAS.Domain.Enums.Inventory;

public enum StockReservationStatus : byte
{
    Active = 1,
    Released = 2,
    Consumed = 3,
    Cancelled = 4
}
