using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Accounting.PostingProfiles;

/// <summary>
/// UI labels for the Module/DocumentType/Role tokens actually consumed by OAS posting services.
/// Do not translate the persisted tokens; unknown historical values remain visible in read mode.
/// </summary>
public static class PostingProfileUiCatalog
{
    public sealed record Document(string Module, string Value, string Text);
    public sealed record Role(string Value, string Text, bool AlwaysRequired, string Hint);

    public static readonly IReadOnlyList<UiSelectOption> Modules =
    [
        new("Sales", "المبيعات"),
        new("Purchasing", "المشتريات"),
        new("Accounting", "المحاسبة"),
        new("Optical", "المعمل البصري")
    ];

    private static readonly IReadOnlyList<Document> Documents =
    [
        new("Sales", "SalesInvoice", "فاتورة مبيعات"),
        new("Purchasing", "PurchaseReceipt", "استلام مشتريات"),
        new("Purchasing", "PurchaseInvoice", "فاتورة مشتريات"),
        new("Accounting", "CustomerAdvanceApplication", "تطبيق عربون عميل"),
        new("Optical", "OpticalBreakage", "تلف أو كسر المعمل")
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<Role>> RoleMap =
        new Dictionary<string, IReadOnlyList<Role>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sales/SalesInvoice"] =
            [
                new("SalesRevenue", "إيرادات المبيعات", true, "حساب إيرادات المبيعات"),
                new("TaxPayable", "ضريبة المبيعات", false, "يُستخدم عندما تتضمن الفاتورة ضريبة"),
                new("Inventory", "المخزون", false, "للأصناف التي تؤثر في المخزون"),
                new("COGS", "تكلفة المبيعات", false, "لتكلفة الأصناف المباعة")
            ],
            ["Purchasing/PurchaseReceipt"] =
            [
                new("Inventory", "المخزون", true, "حساب قيمة المخزون المستلم"),
                new("GoodsReceivedNotInvoiced", "بضاعة مستلمة غير مفوترة", true, "حساب التسوية بين الاستلام والفاتورة")
            ],
            ["Purchasing/PurchaseInvoice"] =
            [
                new("GoodsReceivedNotInvoiced", "بضاعة مستلمة غير مفوترة", true, "حساب تسوية الاستلام"),
                new("PurchaseTax", "ضريبة المشتريات", false, "عند وجود ضريبة مشتريات"),
                new("PurchasePriceVariance", "فروق أسعار المشتريات", false, "عند حدوث فروق في أسعار المطابقة")
            ],
            ["Accounting/CustomerAdvanceApplication"] =
            [
                new("CustomerAdvances", "عربونات العملاء", true, "حساب التزامات عربونات العملاء")
            ],
            ["Optical/OpticalBreakage"] =
            [
                new("ProductionDamageExpense", "مصروف التلف والكسر", true, "حساب مصروف التلف أو الكسر"),
                new("Inventory", "المخزون", true, "حساب المخزون المتأثر بالتلف")
            ]
        };

    public static IReadOnlyList<UiSelectOption> DocumentOptions(string? module) =>
        Documents.Where(x => string.Equals(x.Module, module, StringComparison.OrdinalIgnoreCase))
            .Select(x => new UiSelectOption(x.Value, x.Text)).ToArray();

    public static IReadOnlyList<Role> Roles(string? module, string? documentType) =>
        RoleMap.TryGetValue($"{module}/{documentType}", out var roles) ? roles : [];

    public static IReadOnlyList<UiSelectOption> RoleOptions(string? module, string? documentType) =>
        Roles(module, documentType).Select(x => new UiSelectOption(x.Value, x.Text)).ToArray();

    public static string ModuleText(string? value) =>
        Modules.FirstOrDefault(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase))?.Text
        ?? value ?? "غير محددة";

    public static string DocumentText(string? value) =>
        Documents.FirstOrDefault(x => string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase))?.Text
        ?? value ?? "غير محدد";

    public static string RoleText(string? value) =>
        RoleMap.Values.SelectMany(x => x).FirstOrDefault(x =>
            string.Equals(x.Value, value, StringComparison.OrdinalIgnoreCase))?.Text
        ?? value ?? "غير محدد";

    public static bool IsKnownScope(string? module, string? documentType) =>
        Documents.Any(x => string.Equals(x.Module, module, StringComparison.OrdinalIgnoreCase) &&
                           string.Equals(x.Value, documentType, StringComparison.OrdinalIgnoreCase));
}
