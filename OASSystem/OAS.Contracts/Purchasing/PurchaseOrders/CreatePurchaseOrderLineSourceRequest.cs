namespace OAS.Contracts.Purchasing.PurchaseOrders;

public sealed record CreatePurchaseOrderLineSourceRequest(
    Guid PurchaseRequestLineId,
    decimal AllocatedQuantity);
