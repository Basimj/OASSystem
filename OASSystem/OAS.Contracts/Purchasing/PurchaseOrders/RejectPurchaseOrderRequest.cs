namespace OAS.Contracts.Purchasing.PurchaseOrders;

public sealed record RejectPurchaseOrderRequest(string Reason, string RowVersion);
