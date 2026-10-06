namespace OAS.Contracts.Purchasing.CustomerDemand;

public sealed record CreateCustomerDemandPurchaseOrderRequest(
    Guid SupplierId,
    Guid? ExistingDraftPurchaseOrderId,
    DateOnly? ExpectedDeliveryDate,
    string RowVersion);
