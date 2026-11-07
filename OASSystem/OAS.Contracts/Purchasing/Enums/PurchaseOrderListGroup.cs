namespace OAS.Contracts.Purchasing.Enums;

/// <summary>
/// Server-side groups for the purchase-order overview. Filtering precedes paging.
/// </summary>
public enum PurchaseOrderListGroup : byte
{
    PendingApproval = 1,
    InSupply = 2,
    Completed = 3
}
