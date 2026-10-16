using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Sales.Returns.Services;

/// <summary>
/// Resolves the exact historical accounts used by a posted sales invoice.
/// Resolution is based on immutable posting roles and source-line lineage, never localized descriptions
/// or the posting profile that happens to be active when a return is posted.
/// </summary>
public static class SalesReturnHistoricalAccountResolver
{
    public static Guid ResolveCustomerAccount(IReadOnlyList<JournalEntryLine> lines, Guid customerId)
    {
        var byRole = lines
            .Where(x => string.Equals(x.PostingRole, AccountingPostingRoles.SalesCustomerReceivable, StringComparison.Ordinal))
            .Select(x => x.AccountId)
            .Distinct()
            .ToArray();
        if (byRole.Length == 1) return byRole[0];
        if (byRole.Length > 1)
            throw new ConflictException("sales_return_source_customer_account_ambiguous", "قيد الفاتورة الأصلية يحتوي أكثر من حساب عميل معرف بالدور المحاسبي.");

        // Safe legacy reconstruction: the original sales posting debits exactly one customer header line.
        var legacy = lines
            .Where(x => x.CustomerId == customerId && x.DebitAmount > 0m && x.SourceDocumentLineId == null)
            .Select(x => x.AccountId)
            .Distinct()
            .ToArray();
        if (legacy.Length == 1) return legacy[0];
        throw new ConflictException("sales_return_source_customer_account_missing", "تعذر تحديد حساب العميل التاريخي من قيد الفاتورة الأصلية.");
    }

    public static Guid ResolveRevenueAccount(IReadOnlyList<JournalEntryLine> lines, Guid customerAccount, bool hasTax)
    {
        var byRole = lines
            .Where(x => string.Equals(x.PostingRole, AccountingPostingRoles.SalesRevenue, StringComparison.Ordinal))
            .Select(x => x.AccountId)
            .Distinct()
            .ToArray();
        if (byRole.Length == 1) return byRole[0];
        if (byRole.Length > 1)
            throw new ConflictException("sales_return_source_revenue_account_ambiguous", "قيد الفاتورة الأصلية يحتوي أكثر من حساب إيراد معرف بالدور المحاسبي.");

        // Only a tax-free legacy invoice with one non-customer header credit is reconstructable
        // without guessing. Taxed/ambiguous legacy documents are intentionally sent for review.
        if (!hasTax)
        {
            var legacy = lines
                .Where(x => x.SourceDocumentLineId == null && x.CreditAmount > 0m && x.AccountId != customerAccount)
                .Select(x => x.AccountId)
                .Distinct()
                .ToArray();
            if (legacy.Length == 1) return legacy[0];
        }

        throw new ConflictException(
            "sales_return_legacy_revenue_role_review_required",
            "تعذر إثبات حساب إيراد الفاتورة التاريخي بنيويًا. يلزم مراجعة/تسوية بيانات القيد القديم قبل ترحيل المرتجع.");
    }

    public static Guid ResolveRequiredRoleAccount(
        IReadOnlyList<JournalEntryLine> lines,
        string role,
        string code,
        string message)
    {
        var accounts = lines
            .Where(x => string.Equals(x.PostingRole, role, StringComparison.Ordinal))
            .Select(x => x.AccountId)
            .Distinct()
            .ToArray();
        if (accounts.Length == 1) return accounts[0];
        throw new ConflictException(code, message);
    }

    public static Guid ResolveSourceLineAccount(
        IReadOnlyList<JournalEntryLine> lines,
        Guid sourceInvoiceLineId,
        string role,
        bool creditDirection,
        string code,
        string message)
    {
        var roleAccounts = lines
            .Where(x => x.SourceDocumentLineId == sourceInvoiceLineId &&
                        string.Equals(x.PostingRole, role, StringComparison.Ordinal))
            .Select(x => x.AccountId)
            .Distinct()
            .ToArray();
        if (roleAccounts.Length == 1) return roleAccounts[0];
        if (roleAccounts.Length > 1) throw new ConflictException(code, message);

        // Safe legacy reconstruction for inventory lines. SourceDocumentLineId identifies one sold
        // invoice line; debit is COGS and credit is Inventory in the existing historical posting model.
        var legacy = lines
            .Where(x => x.SourceDocumentLineId == sourceInvoiceLineId &&
                        (creditDirection ? x.CreditAmount > 0m : x.DebitAmount > 0m))
            .Select(x => x.AccountId)
            .Distinct()
            .ToArray();
        if (legacy.Length == 1) return legacy[0];
        throw new ConflictException(code, message);
    }
}
