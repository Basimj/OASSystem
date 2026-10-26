using System.Threading.Tasks;

namespace OAS.Client.Accounting.Pages;

/// <summary>
/// Persists the walk-in customer and default customer receivables control account
/// alongside the standard accounting settings from the existing Save button.
/// </summary>
public partial class AccountingSetupPage
{
    private WalkInCashSettingsField? _walkInSettingsField;

    private async Task SaveSettingsWithWalkInAsync()
    {
        if (_walkInSettingsField is null || !_walkInSettingsField.IsLoaded)
        {
            Snackbar.Warning("تعذر تحميل إعدادات العميل النقدي أو حساب ذمم العملاء. سيتم حفظ بقية إعدادات المحاسبة فقط؛ حدّث قاعدة البيانات وأعد فتح الصفحة.");
            await SaveSettingsAsync();
            return;
        }

        // Persist base settings first; do not save the new defaults when this fails.
        var previousSettings = _settings;
        await SaveSettingsAsync();
        if (ReferenceEquals(previousSettings, _settings))
            return;

        // Both supplemental account settings are saved by the field below.
        if (!await _walkInSettingsField.SaveAsync())
            Snackbar.Warning("تم حفظ إعدادات المحاسبة الأساسية، لكن لم تكتمل إعدادات العميل النقدي أو حساب ذمم العملاء. راجع التنبيه وأعد الحفظ.");
    }
}
