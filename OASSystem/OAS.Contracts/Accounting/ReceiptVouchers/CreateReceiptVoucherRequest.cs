namespace OAS.Contracts.Accounting.ReceiptVouchers;
public sealed record CreateReceiptVoucherRequest(DateOnly VoucherDate,string? Description,IReadOnlyList<CreateReceiptVoucherLineRequest> Lines,string? VoucherNumber=null);
