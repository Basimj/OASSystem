using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Posting;

/// <summary>
/// Shared read-only validation for supplier postings and the AP-10 readiness report.
/// Existing subledger ids never fall back to a generic liability/control account.
/// </summary>
public static class SupplierSubledgerPostingPolicy
{
    public static (string Code, string Message)? Validate(Account account, Account? parent)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (!account.CanReceivePosting() || account.AccountType != AccountType.Subledger ||
            account.AccountClass != AccountClass.Liability || account.NormalBalance != NormalBalance.Credit)
            return ("accounting_supplier_account_invalid", "حساب المورد يجب أن يكون حساب ذمم دائن فعالًا وقابلًا للترحيل.");
        if (!account.ParentAccountId.HasValue || parent is null || parent.Id != account.ParentAccountId.Value ||
            !parent.IsActive || !parent.IsControlAccount || parent.AccountType != AccountType.Control ||
            parent.AccountClass != AccountClass.Liability || parent.NormalBalance != NormalBalance.Credit)
            return ("accounting_supplier_control_invalid", "حساب المورد يجب أن يتبع مباشرةً حساب رقابة موردين فعالًا ضمن الخصوم.");
        return null;
    }
}
