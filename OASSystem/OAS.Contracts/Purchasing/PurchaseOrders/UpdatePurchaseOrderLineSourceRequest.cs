namespace OAS.Contracts.Purchasing.PurchaseOrders;

public sealed record UpdatePurchaseOrderLineSourceRequest(
    Guid? Id,
    Guid PurchaseRequestLineId,
    decimal AllocatedQuantity,
    string? RowVersion);
