using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.ReceiptVouchers;
public sealed record ReceiptVoucherDto(
    Guid Id,string VoucherNumber,DateOnly VoucherDate,
    Guid? BaseCurrencyId,string? BaseCurrencyCodeSnapshot,byte? BaseCurrencyDecimalPlacesSnapshot,decimal? BaseTotalAmount,
    ReceiptVoucherStatus Status,string? Description,Guid? JournalEntryId,string? CreatedBy,DateTimeOffset CreatedAtUtc,Guid? PostedBy,DateTimeOffset? PostedAtUtc,string RowVersion,
    IReadOnlyList<ReceiptVoucherLineDto> Lines);
