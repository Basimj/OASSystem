namespace OAS.Contracts.Accounting.ReceiptVouchers;

public sealed record ReceiptVoucherLineDto(
    Guid Id,
    Guid ReceiptVoucherId,
    int LineNumber,
    Guid AccountId,
    decimal Amount,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Description);