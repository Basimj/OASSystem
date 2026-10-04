namespace OAS.Contracts.Purchasing.PurchaseRequests;

public sealed record CreatePurchaseRequestLineRequest(
    int LineSequence,
    Guid ProductVariantId,
    decimal RequestedQuantity,
    DateOnly? RequiredDate,
    Guid? CustomerOrderLineId,
    Guid? PreferredSupplierId,
    string? Notes,
    DateTimeOffset? ScheduledOrderAtUtc = null);
