using System.Threading.Tasks;

namespace OAS.Client.Accounting.Pages;

public partial class AccountingSetupPage
{
    private WalkInCashSettingsField? _walkInSettingsField;

    private async Task SaveWalkInCustomerAsync()
    {
        if (_walkInSettingsField is null || !_walkInSettingsField.IsLoaded)
        {
            Snackbar.Warning("لم تُحمّل إعدادات العميل النقدي. أعد فتح قسم الحسابات.");
            return;
        }
        if (await _walkInSettingsField.SaveCustomerOnlyAsync())
            Snackbar.Success("تم حفظ العميل النقدي الافتراضي.");
    }

    private async Task SaveCustomerReceivablesAsync()
    {
        if (_walkInSettingsField is null || !_walkInSettingsField.IsLoaded)
        {
            Snackbar.Warning("لم تُحمّل إعدادات ذمم العملاء. أعد فتح قسم الحسابات.");
            return;
        }
        if (await _walkInSettingsField.SaveReceivablesOnlyAsync())
            Snackbar.Success("تم حفظ الحساب الرئيسي لذمم العملاء.");
    }
}
