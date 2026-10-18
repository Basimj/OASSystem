namespace OAS.Contracts.Purchasing.PurchaseOrders;

/// <summary>
/// Quantities are in purchase-order units. Invoiced quantity counts only posted,
/// matched invoice allocations; GRNI is not included as a payable invoice.
/// </summary>
public sealed record PurchaseOrderLineProgressDto(
    Guid PurchaseOrderLineId,
    int LineSequence,
    string ProductName,
    decimal Ordered,
    decimal Received,
    decimal Invoiced,
    decimal Returned,
    decimal RemainingToReceive,
    decimal RemainingToInvoice,
    decimal RemainingToReturn);

public sealed record PurchaseOrderProgressDto(
    Guid PurchaseOrderId,
    string PurchaseOrderCode,
    IReadOnlyList<PurchaseOrderLineProgressDto> Lines);
