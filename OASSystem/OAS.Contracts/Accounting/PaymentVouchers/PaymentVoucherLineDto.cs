namespace OAS.Contracts.Accounting.PaymentVouchers;

public sealed record PaymentVoucherLineDto(
    Guid Id,
    Guid PaymentVoucherId,
    int LineNumber,
    Guid AccountId,
    decimal Amount,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Description);