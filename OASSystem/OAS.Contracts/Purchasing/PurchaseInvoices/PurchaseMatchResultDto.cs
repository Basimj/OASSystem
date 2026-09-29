using OAS.Contracts.Purchasing.Enums;

namespace OAS.Contracts.Purchasing.PurchaseInvoices;

public sealed record PurchaseMatchResultDto(
    Guid PurchaseInvoiceId,
    PurchaseMatchStatus OverallStatus,
    bool RequiresApproval,
    IReadOnlyList<PurchaseInvoiceReceiptAllocationDto> Allocations,
    IReadOnlyList<PurchaseVarianceDto> Variances);
