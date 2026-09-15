using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.PaymentVouchers;

public sealed record UpdatePaymentVoucherRequest(
    DateOnly VoucherDate,
    PaymentPartyType PartyType,
    Guid? SupplierId,
    string? BeneficiaryName,
    PaymentMethod PaymentMethod,
    Guid? CashAccountId,
    Guid? BankAccountId,
    decimal TotalAmount,
    string? Description,
    IReadOnlyList<CreatePaymentVoucherLineRequest> Lines,
    string RowVersion);