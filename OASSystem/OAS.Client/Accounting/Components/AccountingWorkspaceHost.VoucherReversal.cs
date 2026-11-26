using OAS.Client.Accounting.Workspace;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.Journals;
using OAS.Contracts.Accounting.Reports;
using OAS.UiLib.Components.Accounting.Vouchers;

namespace OAS.Client.Accounting.Components;

public partial class AccountingWorkspaceHost
{
    private bool IsActivePostedVoucher => Workspace.ActiveTab?.Model is
        ReceiptVoucherEditor.FormModel { Status: ReceiptVoucherStatus.Posted, JournalEntryId: not null } or
        PaymentVoucherEditor.FormModel { Status: PaymentVoucherStatus.Posted, JournalEntryId: not null };
    private Guid? ActiveVoucherReversalId => Workspace.ActiveTab?.Model switch
    {
        ReceiptVoucherEditor.FormModel r => r.ReversalJournalId,
        PaymentVoucherEditor.FormModel p => p.ReversalJournalId,
        _ => null
    };
    private bool ActiveVoucherJournalLoaded => Workspace.ActiveTab?.Model switch
    {
        ReceiptVoucherEditor.FormModel r => r.JournalLoaded,
        PaymentVoucherEditor.FormModel p => p.JournalLoaded,
        _ => false
    };
    private bool CanAllocateActiveVoucher => IsActivePostedVoucher && ActiveVoucherJournalLoaded &&
        ActiveVoucherReversalId is null && Workspace.ActiveTab is { IsNew: false, IsDirty: false, IsSaving: false, IsLoading: false };

    private async Task<JournalEntryDto?> LoadVoucherJournalAsync(Guid? id)
    {
        if (id is not Guid journalId) return null;
        try { return await AccountingService.GetJournalByIdAsync(journalId); }
        catch (Exception) { return null; } // Fail closed: refresh before attempting reversal.
    }

    private async Task ViewVoucherReversalAsync()
    {
        if (ActiveVoucherReversalId is Guid id)
            await OpenRecordInTab(AccountingEntityType.Journals, id, "القيد العكسي");
    }

    private async Task ReverseActiveVoucherAsync()
    {
        if (!IsActivePostedVoucher || !ActiveVoucherJournalLoaded || ActiveVoucherReversalId.HasValue ||
            Workspace.ActiveTab is not { EntityId: Guid id, IsSaving: false, IsLoading: false, IsDirty: false, IsEditMode: false } tab) return;
        var receipt = tab.Model as ReceiptVoucherEditor.FormModel;
        var payment = tab.Model as PaymentVoucherEditor.FormModel;
        var originalDate = receipt?.OriginalPostingDate ?? payment?.OriginalPostingDate;
        if (!originalDate.HasValue) { Snackbar.Warning("تعذر تحديد تاريخ القيد الأصلي. حدّث السند أولاً."); return; }
        tab.IsSaving = true;
        Workspace.NotifyStateChanged();
        try
        {
            var result = await Dialog.ShowAsync<VoucherReversalDialog>("عكس السند", new Dictionary<string, object> { [nameof(VoucherReversalDialog.OriginalDate)] = originalDate.Value.ToDateTime(TimeOnly.MinValue), [nameof(VoucherReversalDialog.VoucherId)] = id, [nameof(VoucherReversalDialog.IsReceipt)] = receipt is not null });
            if (result.Cancelled || result.Value is not ReverseVoucherRequest request) return;
            var kind = receipt is not null ? "receipt" : "payment";
            var reversalId = await VoucherFundsApi.PostAsync<ReverseVoucherRequest, Guid>($"api/accounting/{kind}-vouchers/{id:D}/reverse", request);
            if (reversalId == Guid.Empty) throw new InvalidOperationException("لم يرجع الخادم رقم القيد العكسي.");
            // Preserve the confirmed result even if refreshing the view subsequently fails.
            if (receipt is not null) { receipt.ReversalJournalId = reversalId; receipt.ReversalPosted = true; }
            if (payment is not null) { payment.ReversalJournalId = reversalId; payment.ReversalPosted = true; }
            Snackbar.Success("تم عكس السند وترحيل القيد العكسي.");
            await LoadExistingRecordAsync(tab, id);
            await LoadEntityListAsync(tab.EntityType);
        }
        catch (ApiClientException ex) { ShowAccountingError(ex.Error); }
        catch (Exception) { ApiFeedback.ShowUnexpected(); }
        finally { tab.IsSaving = false; Workspace.NotifyStateChanged(); }
    }
}
