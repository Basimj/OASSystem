namespace OAS.Domain.Accounting.Enums;

public enum AllocationTargetDocumentType : byte
{
    SalesInvoice = 1,
    PurchaseInvoice = 2,
    DebitAdjustment = 3,
    CreditAdjustment = 4
}