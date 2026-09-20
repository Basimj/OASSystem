using OAS.Contracts.Enums.Inventory;
using OAS.UiLib.Core.Models;

namespace OAS.Client.Inventory.Common;

public static class InventoryArabicPresenter
{
    public static readonly IReadOnlyList<UiSelectOption> TransactionTypeOptions =
    [
        new(((int)InventoryTransactionType.Opening).ToString(), "رصيد افتتاحي"),
        new(((int)InventoryTransactionType.Receipt).ToString(), "إدخال مخزون"),
        new(((int)InventoryTransactionType.Issue).ToString(), "إخراج مخزون"),
        new(((int)InventoryTransactionType.Transfer).ToString(), "تحويل بين المخازن"),
        new(((int)InventoryTransactionType.AdjustmentIncrease).ToString(), "تعديل زيادة"),
        new(((int)InventoryTransactionType.AdjustmentDecrease).ToString(), "تعديل نقص"),
        new(((int)InventoryTransactionType.PurchaseReturn).ToString(), "مرتجع شراء"),
        new(((int)InventoryTransactionType.SalesReturn).ToString(), "مرتجع بيع"),
        new(((int)InventoryTransactionType.Scrap).ToString(), "تالف / Scrap")
    ];

    public static readonly IReadOnlyList<UiSelectOption> TransactionStatusOptions =
    [
        new(string.Empty, "كل الحالات"),
        new(((int)InventoryTransactionStatus.Draft).ToString(), "مسودة"),
        new(((int)InventoryTransactionStatus.Posted).ToString(), "مرحّلة")
    ];

    public static readonly IReadOnlyList<UiSelectOption> MovementTypeOptions =
    [
        new(string.Empty, "كل الحركات"),
        new(((int)InventoryMovementType.In).ToString(), "وارد"),
        new(((int)InventoryMovementType.Out).ToString(), "صادر")
    ];

    public static readonly IReadOnlyList<UiSelectOption> StockCountStatusOptions =
    [
        new(string.Empty, "كل الحالات"),
        new(((int)StockCountStatus.Draft).ToString(), "مسودة"),
        new(((int)StockCountStatus.Counting).ToString(), "قيد العد"),
        new(((int)StockCountStatus.Review).ToString(), "مراجعة"),
        new(((int)StockCountStatus.Approved).ToString(), "معتمد"),
        new(((int)StockCountStatus.Posted).ToString(), "مرحّل"),
        new(((int)StockCountStatus.Cancelled).ToString(), "ملغي")
    ];

    public static string TransactionTypeText(InventoryTransactionType type) => type switch
    {
        InventoryTransactionType.Opening => "رصيد افتتاحي",
        InventoryTransactionType.Receipt => "إدخال مخزون",
        InventoryTransactionType.Issue => "إخراج مخزون",
        InventoryTransactionType.Transfer => "تحويل بين المخازن",
        InventoryTransactionType.AdjustmentIncrease => "تعديل زيادة",
        InventoryTransactionType.AdjustmentDecrease => "تعديل نقص",
        InventoryTransactionType.SalesReturn => "مرتجع بيع",
        InventoryTransactionType.PurchaseReturn => "مرتجع شراء",
        InventoryTransactionType.ProductionIssue => "صرف إنتاج",
        InventoryTransactionType.Scrap => "تالف / Scrap",
        _ => type.ToString()
    };

    public static string TransactionStatusText(InventoryTransactionStatus status) => status switch
    {
        InventoryTransactionStatus.Draft => "مسودة",
        InventoryTransactionStatus.Posted => "مرحّلة",
        _ => status.ToString()
    };

    public static string MovementTypeText(InventoryMovementType type) => type switch
    {
        InventoryMovementType.In => "وارد",
        InventoryMovementType.Out => "صادر",
        _ => type.ToString()
    };

    public static string StockCountStatusText(StockCountStatus status) => status switch
    {
        StockCountStatus.Draft => "مسودة",
        StockCountStatus.Counting => "قيد العد",
        StockCountStatus.Review => "مراجعة",
        StockCountStatus.Approved => "معتمد",
        StockCountStatus.Posted => "مرحّل",
        StockCountStatus.Cancelled => "ملغي",
        _ => status.ToString()
    };
}
