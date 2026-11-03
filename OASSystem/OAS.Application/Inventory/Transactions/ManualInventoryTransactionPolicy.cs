using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.Transactions;

/// <summary>
/// General inventory transactions cannot supply stock without a matching financial document.
/// Purchase receipts and returns use their own posting services; inter-warehouse transfers
/// move existing units without increasing total stock.
/// </summary>
public static class ManualInventoryTransactionPolicy
{
    public static bool IsAllowed(InventoryTransactionType type) => type is
        InventoryTransactionType.Issue or
        InventoryTransactionType.Transfer or
        InventoryTransactionType.AdjustmentDecrease or
        InventoryTransactionType.PurchaseReturn or
        InventoryTransactionType.ProductionIssue or
        InventoryTransactionType.Scrap;

    public const string ErrorCode = "manual_inventory_inbound_disabled";
    public const string ErrorMessage = "لا يمكن إدخال كمية جديدة مباشرة في المخزون. يجب استلام المشتريات وترحيل سند الاستلام أولاً.";
}
