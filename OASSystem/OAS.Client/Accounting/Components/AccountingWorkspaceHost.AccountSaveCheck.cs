using OAS.Contracts.Accounting.Accounts;

namespace OAS.Client.Accounting.Components;

public partial class AccountingWorkspaceHost
{
    private static UpdateAccountRequest BuildAccountUpdateRequest(AccountEditor.FormModel model)
        => new(
            model.Code.Trim(),
            model.NameAr.Trim(),
            NullIfBlank(model.NameEn),
            model.ParentAccountId,
            checked((byte)model.Level),
            model.AccountClass,
            model.AccountType,
            model.NormalBalance,
            model.IsPostingAccount,
            model.IsControlAccount,
            model.AllowManualPosting,
            model.IsSystemAccount,
            model.IsActive,
            model.EffectiveDate,
            model.RowVersion);

    /// <summary>
    /// Called immediately before PUT, using the very same captured UpdateAccountRequest
    /// for both the read-only save check and the subsequent update.
    /// The throwing server validation is intentionally retained for races/direct callers.
    /// </summary>
    private async Task<bool> ValidateAccountBeforeSaveAsync(Guid accountId, UpdateAccountRequest proposedUpdate)
    {
        if (string.IsNullOrWhiteSpace(proposedUpdate.Code) || string.IsNullOrWhiteSpace(proposedUpdate.NameAr))
        {
            Snackbar.Warning("رقم الحساب والاسم العربي مطلوبان.");
            return false;
        }
        if (proposedUpdate.Level == 0 || string.IsNullOrWhiteSpace(proposedUpdate.RowVersion))
        {
            Snackbar.Warning("مستوى الحساب أو بيانات التزامن غير صالحة؛ أعد فتح الحساب.");
            return false;
        }

        var check = await AccountingService.GetAccountSaveCheckAsync(accountId, proposedUpdate);

        if (check is null)
        {
            Snackbar.Error("تعذر التحقق من الحساب. لم يُرسل طلب الحفظ؛ حاول مرة أخرى.");
            return false;
        }
        if (check.CanSave)
            return true;

        Snackbar.Warning(GetAccountPrecheckMessage(check.ReasonCode));
        // Preserve the user's edits; they can correct the form before retrying.
        return false;
    }

    private static string GetAccountPrecheckMessage(string? reason) => reason switch
    {
        "accounting_managed_account_edit_forbidden" => "هذا حساب مرتبط بعميل أو مورد أو موظف أو صندوق أو بنك؛ عدّله من شاشة السجل المرتبط به.",
        "accounting_system_account_deactivation_forbidden" => "لا يمكن تعطيل حساب نظامي. أبقِ خيار «نشط» مفعّلًا.",
        "accounting_mapped_account_deactivation_forbidden" => "لا يمكن تعطيل الحساب لأنه مستخدم في إعدادات الربط أو ملفات الترحيل؛ غيّر الربط أولًا.",
        "accounting_account_active_children_exist" => "لا يمكن تعطيل الحساب لوجود حسابات فرعية نشطة تحته.",
        "accounting_account_history_structure_immutable" => "لا يمكن تغيير قبول الترحيل أو تصنيف الحساب أو نوعه أو طبيعته بعد تسجيل حركات قيود عليه.",
        "accounting_mapped_account_structure_immutable" => "لا يمكن تغيير إعدادات الترحيل أو البنية المحاسبية لحساب نظامي أو مستخدم في ربط محاسبي.",
        "accounting_system_account_flag_immutable" => "لا يمكن تغيير صفة «حساب نظامي» بعد إنشاء الحساب.",
        "accounting_account_code_immutable" => "لا يمكن تغيير رمز الحساب بعد إنشائه.",
        "accounting_account_root_level_invalid" => "يجب أن يكون مستوى الحساب الرئيسي 1.",
        "accounting_account_self_parent" or "accounting_account_parent_cycle" => "لا يمكن جعل الحساب تابعًا لنفسه أو لأحد فروعه.",
        "accounting_account_existing_parent_cycle" or "accounting_account_parent_chain_broken" => "توجد مشكلة في تسلسل الحسابات الأب؛ راجع الشجرة قبل الحفظ.",
        "accounting_account_parent_not_found" => "الحساب الأب المحدد غير موجود؛ اختر حسابًا صحيحًا.",
        "accounting_account_parent_inactive" => "الحساب الأب غير نشط؛ اختر حسابًا أب نشطًا.",
        "accounting_account_level_parent_mismatch" => "مستوى الحساب لا يطابق مستوى الحساب الأب.",
        "accounting_account_header_posting_invalid" => "لا يسمح الحساب التجميعي بقبول الترحيل.",
        "accounting_account_header_manual_invalid" => "لا يسمح الحساب التجميعي بالترحيل اليدوي.",
        "accounting_account_manual_without_posting_invalid" => "يجب تفعيل «يقبل الترحيل» أولًا قبل السماح بالترحيل اليدوي.",
        "accounting_account_control_flag_required" => "الحساب التحكمي يجب أن يكون معلّمًا كحساب تحكمي.",
        "accounting_account_classification_invalid" => "تصنيف الحساب أو نوعه أو طبيعته غير صحيح.",
        "accounting_account_stale_row_version" => "عدّل مستخدم آخر هذا الحساب؛ حدّث البيانات قبل إعادة الحفظ.",
        "accounting_account_row_version_invalid" => "بيانات تزامن الحساب غير صالحة؛ أعد فتح الحساب.",
        "accounting_account_not_found" => "لم يعد الحساب موجودًا؛ حدّث شجرة الحسابات.",
        "accounting_account_name_required" => "اسم الحساب بالعربية مطلوب.",
        "accounting_account_fields_too_long" => "هناك حقل أطول من الحد المسموح؛ راجع رمز الحساب والأسماء.",
        "accounting_account_level_invalid" => "مستوى الحساب غير صالح.",
        "accounting_account_parent_invalid" => "الحساب الأب المحدد غير صالح.",
        _ => "تعذر حفظ الحساب لأن التعديل يخالف إحدى قواعد المحاسبة؛ راجع بيانات الحساب وحاول مرة أخرى."
    };
}
