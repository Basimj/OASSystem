namespace OAS.Domain.Accounting.Enums;

public enum PaymentMethod : byte
{
    Cash = 1,
    Card = 2,
    BankTransfer = 3,
    Cheque = 4,
    Other = 5
}