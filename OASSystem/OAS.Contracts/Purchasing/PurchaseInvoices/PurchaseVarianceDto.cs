using OAS.Contracts.Purchasing.Enums;

namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record PurchaseVarianceDto(
    PurchaseVarianceType Type,
    Guid PurchaseInvoiceLineId,
    Guid? PurchaseReceiptLineId,
    decimal ExpectedValue,
    decimal ActualValue,
    decimal Variance,
    decimal Tolerance,
    PurchaseMatchStatus Status,
    string? Message);
