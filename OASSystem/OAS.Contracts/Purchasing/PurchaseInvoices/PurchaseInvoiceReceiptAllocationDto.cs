using OAS.Contracts.Purchasing.Enums;

namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record PurchaseInvoiceReceiptAllocationDto(
    Guid Id,
    Guid PurchaseInvoiceLineId,
    Guid PurchaseReceiptLineId,
    string? ReceiptCode,
    decimal MatchedQuantity,
    decimal MatchedNetAmount,
    decimal QuantityVariance,
    decimal PriceVarianceAmount,
    decimal TaxVarianceAmount,
    PurchaseMatchStatus MatchStatus,
    string? ApprovalReason,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    string RowVersion);
