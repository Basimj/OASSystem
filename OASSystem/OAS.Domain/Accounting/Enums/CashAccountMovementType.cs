namespace OAS.Domain.Accounting.Enums;

public enum CashAccountMovementType : byte
{
    ReceiptOnly = 1,
    PaymentOnly = 2,
    ReceiptAndPayment = 3
}
