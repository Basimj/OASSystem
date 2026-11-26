using OAS.Contracts.Accounting.Enums;
using OAS.UiLib.Core.Models;
namespace OAS.Client.Accounting.Common;

public static class FinancialStatementCategoryPresenter
{
    public static string Label(FinancialStatementCategory? category) => category switch
    {
        FinancialStatementCategory.CurrentAssets => "أصول متداولة",
        FinancialStatementCategory.NonCurrentAssets => "أصول غير متداولة",
        FinancialStatementCategory.CurrentLiabilities => "التزامات متداولة",
        FinancialStatementCategory.NonCurrentLiabilities => "التزامات غير متداولة",
        FinancialStatementCategory.Equity => "حقوق الملكية",
        FinancialStatementCategory.OperatingRevenue => "إيرادات تشغيلية",
        FinancialStatementCategory.RevenueReductions => "تخفيضات الإيرادات",
        FinancialStatementCategory.OtherRevenue => "إيرادات أخرى",
        FinancialStatementCategory.CostOfSales => "تكلفة المبيعات",
        FinancialStatementCategory.OperatingExpenses => "مصروفات تشغيلية",
        FinancialStatementCategory.OtherExpenses => "مصروفات أخرى",
        FinancialStatementCategory.IncomeTax => "ضريبة الدخل",
        _ => "غير مصنف"
    };
    public static IReadOnlyList<UiSelectOption> Options(AccountClass cls, string inheritLabel) =>
        new[] { new UiSelectOption("inherit", inheritLabel) }.Concat(Enum.GetValues<FinancialStatementCategory>()
            .Where(x => FinancialStatementCategories.IsCompatible(cls, x)).Select(x => new UiSelectOption(x.ToString(), Label(x)))).ToArray();
}
