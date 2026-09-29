namespace OAS.Contracts.Purchasing.PurchaseOrders;

public sealed record CancelPurchaseOrderRequest(string? Reason, string RowVersion);
