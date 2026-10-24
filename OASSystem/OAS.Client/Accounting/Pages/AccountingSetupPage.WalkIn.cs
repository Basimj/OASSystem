using System.Threading.Tasks;

namespace OAS.Client.Accounting.Pages;

/// <summary>
/// Keeps walk-in customer settings next to the regular accounting options, and
/// persists them from the existing accounting settings Save action.
/// </summary>
public partial class AccountingSetupPage
{
    private WalkInCashSettingsField? _walkInSettingsField;

    private async Task SaveSettingsWithWalkInAsync()
    {
        if (_walkInSettingsField is null || !_walkInSettingsField.IsLoaded)
        {
            Snackbar.Warning("تعذر تحميل إعداد العميل النقدي؛ سيتم حفظ بقية الإعدادات فقط. تأكد من تحديث قاعدة البيانات ثم أعد تحميل الصفحة.");
            await SaveSettingsAsync();
            return;
        }

        // The base operation validates all accounting settings and only assigns
        // a new DTO instance to _settings after the API has saved successfully.
        var previousSettings = _settings;
        await SaveSettingsAsync();
        if (ReferenceEquals(previousSettings, _settings))
            return;

        // The walk-in setting uses the existing endpoint/table introduced with
        // walk-in sales.  Do not try to persist it when the base save fails.
        if (!await _walkInSettingsField.SaveAsync())
            Snackbar.Warning("تم حفظ إعدادات المحاسبة الأساسية، لكن تعذّر حفظ اختيار العميل النقدي. راجع الخطأ وأعد المحاولة.");
    }
}
