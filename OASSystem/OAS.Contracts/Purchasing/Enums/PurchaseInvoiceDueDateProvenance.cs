namespace OAS.Contracts.Purchasing.Enums;

public enum PurchaseInvoiceDueDateProvenance : byte
{
    Unknown = 0,
    Explicit = 1,
    DerivedAtInvoice = 2,
    VerifiedHistoricalBackfill = 3
}
