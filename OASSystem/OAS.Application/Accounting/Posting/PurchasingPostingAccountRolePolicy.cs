using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Posting;

/// <summary>
/// Accounting role invariants shared by readiness and actual posting.
/// No hard-coded account ids; the account is selected by an explicit PostingProfileLine.
/// </summary>
public static class PurchasingPostingAccountRolePolicy
{
    public static (string Code, string Message)? Validate(string role, Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (!account.CanReceivePosting() || account.AccountType == AccountType.Subledger)
            return ("purchasing_role_account_invalid", "دور ملف ترحيل المشتريات يجب أن يستخدم حسابًا فعالًا قابلًا للترحيل، وليس حساب ذمم مورد.");
        if (string.Equals(role, "Inventory", StringComparison.OrdinalIgnoreCase) &&
            account.AccountClass != AccountClass.Asset)
            return ("purchasing_inventory_class_invalid", "يجب أن يكون حساب المخزون ضمن الأصول.");
        if (string.Equals(role, "GoodsReceivedNotInvoiced", StringComparison.OrdinalIgnoreCase) &&
            account.AccountClass != AccountClass.Liability)
            return ("purchasing_grni_class_invalid", "يجب أن يكون حساب GRNI ضمن الخصوم.");
        return null;
    }
}
