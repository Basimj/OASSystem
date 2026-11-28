namespace OAS.Contracts.Accounting.CashAccounts;

public enum CashAccountMovementType : byte
{
    ReceiptOnly = 1,
    PaymentOnly = 2,
    ReceiptAndPayment = 3
}
