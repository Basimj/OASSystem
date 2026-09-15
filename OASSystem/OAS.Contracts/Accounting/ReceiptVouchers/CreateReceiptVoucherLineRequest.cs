namespace OAS.Contracts.Accounting.ReceiptVouchers;

public sealed record CreateReceiptVoucherLineRequest(
    Guid AccountId,
    decimal Amount,
    string? ReferenceType = null,
    Guid? ReferenceId = null,
    string? Description = null);