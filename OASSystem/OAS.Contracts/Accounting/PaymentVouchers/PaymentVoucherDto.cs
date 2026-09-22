using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.PaymentVouchers;

public sealed record PaymentVoucherDto(
    Guid Id,
    string VoucherNumber,
    DateOnly VoucherDate,
    PaymentPartyType PartyType,
    Guid? SupplierId,
    string? BeneficiaryName,
    PaymentMethod PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    decimal TotalAmount,
    PaymentVoucherStatus Status,
    string? Description,
    Guid? JournalEntryId,
    string? CreatedBy,
    DateTimeOffset CreatedAtUtc,
    Guid? PostedBy,
    DateTimeOffset? PostedAtUtc,
    string RowVersion,
    IReadOnlyList<PaymentVoucherLineDto> Lines);
