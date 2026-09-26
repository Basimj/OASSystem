namespace OAS.Contracts.Accounting.PaymentVouchers;
public sealed record CreatePaymentVoucherRequest(DateOnly VoucherDate,string? Description,IReadOnlyList<CreatePaymentVoucherLineRequest> Lines,string? VoucherNumber=null);
