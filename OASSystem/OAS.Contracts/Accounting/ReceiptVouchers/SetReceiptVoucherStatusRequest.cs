using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.ReceiptVouchers;

public sealed record SetReceiptVoucherStatusRequest(
    ReceiptVoucherStatus Status,
    string RowVersion);