using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.PaymentVouchers;

public sealed record SetPaymentVoucherStatusRequest(
    PaymentVoucherStatus Status,
    string RowVersion);