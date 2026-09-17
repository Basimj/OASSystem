namespace OAS.Contracts.Enums.Inventory;

public enum InventoryTransactionType
{
    Opening = 1,
    Receipt = 2,
    Issue = 3,
    Transfer = 4,
    AdjustmentIncrease = 5,
    AdjustmentDecrease = 6,
    SalesReturn = 7,
    PurchaseReturn = 8,
    ProductionIssue = 9,
    Scrap = 10
}