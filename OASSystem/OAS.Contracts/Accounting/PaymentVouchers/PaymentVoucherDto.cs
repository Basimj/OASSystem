using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.PaymentVouchers;
public sealed record PaymentVoucherDto(
    Guid Id,string VoucherNumber,DateOnly VoucherDate,
    Guid? BaseCurrencyId,string? BaseCurrencyCodeSnapshot,byte? BaseCurrencyDecimalPlacesSnapshot,decimal? BaseTotalAmount,
    PaymentVoucherStatus Status,string? Description,Guid? JournalEntryId,string? CreatedBy,DateTimeOffset CreatedAtUtc,Guid? PostedBy,DateTimeOffset? PostedAtUtc,string RowVersion,
    IReadOnlyList<PaymentVoucherLineDto> Lines);
