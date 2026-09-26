namespace OAS.Contracts.Accounting.PaymentVouchers;
public sealed record UpdatePaymentVoucherRequest(DateOnly VoucherDate,string? Description,IReadOnlyList<CreatePaymentVoucherLineRequest> Lines,string RowVersion);
