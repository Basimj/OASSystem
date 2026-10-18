namespace OAS.Domain.Sales.Enums;
public enum CustomerCreditSettlementKind : byte
{
    ApplyToInvoice = 1,
    RefundByPaymentVoucherLine = 2
}
public enum CustomerCreditApplicationStatus : byte
{
    Active = 1,
    Reversed = 2
}
