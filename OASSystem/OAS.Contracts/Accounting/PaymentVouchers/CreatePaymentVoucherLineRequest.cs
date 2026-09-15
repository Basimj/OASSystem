namespace OAS.Contracts.Accounting.PaymentVouchers;

public sealed record CreatePaymentVoucherLineRequest(
    Guid AccountId,
    decimal Amount,
    string? ReferenceType = null,
    Guid? ReferenceId = null,
    string? Description = null);