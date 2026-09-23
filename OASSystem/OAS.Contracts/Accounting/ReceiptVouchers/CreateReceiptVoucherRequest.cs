using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.ReceiptVouchers;

public sealed record CreateReceiptVoucherRequest(
    DateOnly VoucherDate,
    ReceiptPartyType PartyType,
    Guid? CustomerId,
    string? ReceivedFrom,
    PaymentMethod PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    decimal TotalAmount,
    string? Description,
    IReadOnlyList<CreateReceiptVoucherLineRequest> Lines,
    string? VoucherNumber = null);