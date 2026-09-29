using OAS.Contracts.Purchasing.Enums;

namespace OAS.Contracts.Purchasing.PurchaseRequests;

public sealed record UpdatePurchaseRequestRequest(
    PurchaseRequestType RequestType,
    Guid WarehouseId,
    Guid? CustomerOrderId,
    DateOnly RequestDate,
    DateOnly? RequiredDate,
    string? Reason,
    string? Notes,
    IReadOnlyList<UpdatePurchaseRequestLineRequest> Lines,
    string RowVersion);
