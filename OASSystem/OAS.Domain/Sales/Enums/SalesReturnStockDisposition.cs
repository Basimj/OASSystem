namespace OAS.Domain.Sales.Enums;

public enum SalesReturnStockDisposition : byte
{
    Unknown = 0,
    Restock = 1,
    Quarantine = 2,
    Dispose = 3,
    NoStock = 4
}
