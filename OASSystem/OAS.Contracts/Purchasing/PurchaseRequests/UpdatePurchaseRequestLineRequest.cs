namespace OAS.Contracts.Purchasing.PurchaseRequests;

public sealed record UpdatePurchaseRequestLineRequest(
    Guid? Id,
    int LineSequence,
    Guid ProductVariantId,
    decimal RequestedQuantity,
    DateOnly? RequiredDate,
    Guid? CustomerOrderLineId,
    Guid? PreferredSupplierId,
    string? Notes,
    string? RowVersion);
