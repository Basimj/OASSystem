namespace OAS.Contracts.Purchasing.PurchaseRequests;

public sealed record SetPurchaseRequestStatusRequest(
    string RowVersion,
    string? Reason = null);
