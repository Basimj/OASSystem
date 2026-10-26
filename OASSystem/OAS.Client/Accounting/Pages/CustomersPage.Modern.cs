using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Settings;
using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;

namespace OAS.Client.Accounting.Pages;

// Small navigation helpers for the new customer list / detail / edit experience.
// The established create/update/status and Excel APIs remain untouched.
public partial class CustomersPage
{
    [Inject] private OasApiClient CustomerSettingsApi { get; set; } = default!;
    private Guid? _startInEditModeFor;

    private async Task OpenCustomerForEditingAsync(Guid id)
    {
        await OpenAsync(id);
        if (ActiveEditorTab?.EntityId == id)
            _startInEditModeFor = id;
    }

    // Creating the accounting account is the server's responsibility. The UI only
    // resolves the designated, eligible receivables parent; it never asks the
    // operator to type or choose an arbitrary ledger account for each customer.
    private async Task NewCustomerWithSuggestedAccountAsync()
    {
        _startInEditModeFor = null;
        await NewAsync();
    }

    private bool _resolvingCustomerParent;

    private async Task SaveCustomerWithAutoParentAsync()
    {
        if (_busy || _resolvingCustomerParent) return;
        if (!IsNew)
        {
            await SaveAsync();
            return;
        }

        _resolvingCustomerParent = true;
        try
        {
            var tab = ActiveEditorTab;
            if (tab is null || !tab.IsNew) return;
            var parentId = await ResolveCustomerParentAsync();
            if (!parentId.HasValue) return;

            // Use the server-generated customer code and the server's existing
            // ProvisionCustomerAccountAsync path for account numbering/linking.
            tab.Form.ParentAccountId = parentId.Value.ToString("D");
            await SaveAsync();
        }
        catch (ApiClientException ex)
        {
            ShowAccountingError(ex.Error);
        }
        catch (Exception)
        {
            ApiFeedback.ShowUnexpected();
        }
        finally
        {
            _resolvingCustomerParent = false;
        }
    }

    private async Task<Guid?> ResolveCustomerParentAsync()
    {
        // Selection happens ONCE in Accounting Settings. Do not infer the
        // parent from arbitrary eligible accounts or Arabic account names.
        var settings = await CustomerSettingsApi.GetAsync<CustomerReceivablesSettingsDto>(
            "api/accounting/customer-receivables-settings");
        if (settings?.ParentAccountId is not { } id)
        {
            Snackbar.Warning("حساب ذمم العملاء غير محدد. افتح إعدادات المحاسبة واختر «الحساب الرئيسي لذمم العملاء» ثم احفظ الإعدادات.");
            return null;
        }

        // Protect against a configured account later being disabled/reclassified.
        if (!await ValidateParentAccountAsync(id)) return null;
        return id;
    }

    private async Task BackToCustomerListAsync()
    {
        var tab = ActiveEditorTab;
        if (tab is not null)
        {
            if (tab.IsDirty)
            {
                var discard = await Dialog.ConfirmAsync(
                    "تغييرات لم تُحفظ",
                    "هناك بيانات عدلتها ولم تحفظها. هل تريد العودة إلى قائمة العملاء دون حفظها؟",
                    AlertTone.Warning,
                    confirmText: "العودة دون حفظ",
                    cancelText: "متابعة التعديل");
                if (!discard) return;
            }

            // The new design has no second tab strip. Remove an unused editor
            // when going back so stale, unsaved forms are never reopened.
            _editorTabs.Remove(tab);
        }
        _activeTabId = _listTabId;
        _startInEditModeFor = null;
    }

    private Task CancelCustomerEditAsync() => BackToCustomerListAsync();

    private async Task ToggleCustomerStatusWithConfirmationAsync()
    {
        var tab = ActiveEditorTab;
        if (tab is null || tab.IsNew) return;
        var action = tab.Form.IsActive ? "إيقاف التعامل مع العميل" : "تفعيل العميل";
        var confirmed = await Dialog.ConfirmAsync(
            "تأكيد حالة العميل",
            $"هل تريد {action} «{tab.Form.NameAr}»؟",
            AlertTone.Warning,
            confirmText: tab.Form.IsActive ? "إيقاف التعامل" : "تفعيل",
            cancelText: "إلغاء");
        if (confirmed) await ToggleStatusAsync();
    }
}
