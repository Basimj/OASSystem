namespace OAS.Contracts.Purchasing.PurchaseOrders;

public sealed record PurchaseOrderLineSourceDto(
    Guid Id,
    Guid PurchaseOrderLineId,
    Guid PurchaseRequestLineId,
    string? PurchaseRequestCode,
    decimal AllocatedQuantity,
    string RowVersion);
