namespace OAS.Contracts.Accounting.ReceiptVouchers;
public sealed record UpdateReceiptVoucherRequest(DateOnly VoucherDate,string? Description,IReadOnlyList<CreateReceiptVoucherLineRequest> Lines,string RowVersion);
