using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.ReceiptVouchers;

public sealed record ReceiptVoucherDto(
    Guid Id,
    string VoucherNumber,
    DateOnly VoucherDate,
    ReceiptPartyType PartyType,
    Guid? CustomerId,
    string? ReceivedFrom,
    PaymentMethod PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    decimal TotalAmount,
    ReceiptVoucherStatus Status,
    string? Description,
    Guid? JournalEntryId,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc,
    Guid? PostedBy,
    DateTimeOffset? PostedAtUtc,
    string RowVersion,
    IReadOnlyList<ReceiptVoucherLineDto> Lines);