using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Sales.Returns.Services;

/// <summary>
/// Resolves the *original* posted invoice accounts, never the active posting profile.
/// Legacy reconstruction is allowed only with unique structural evidence.
/// </summary>
public static class SalesReturnHistoricalAccountResolver
{
    public static Guid ResolveCustomerAccount(IReadOnlyList<JournalEntryLine> lines, Guid customerId)
    {
        var roles = lines.Where(x => x.PostingRole == AccountingPostingRoles.SalesCustomerReceivable)
            .Select(x => x.AccountId).Distinct().ToArray();
        if (roles.Length == 1) return roles[0];
        if (roles.Length > 1)
            throw new ConflictException("sales_return_source_customer_account_ambiguous", "قيد الفاتورة الأصلية يحتوي أكثر من حساب عميل معرف بالدور المحاسبي.");

        var legacy = lines.Where(x => x.CustomerId == customerId && x.DebitAmount > 0m && x.SourceDocumentLineId is null)
            .Select(x => x.AccountId).Distinct().ToArray();
        if (legacy.Length == 1) return legacy[0];
        throw new ConflictException("sales_return_source_customer_account_missing", "تعذر تحديد حساب العميل التاريخي من قيد الفاتورة الأصلية.");
    }

    // Keep the original overload and its conservative semantics for existing callers/tests.
    public static Guid ResolveRevenueAccount(IReadOnlyList<JournalEntryLine> lines, Guid customerAccount, bool hasTax)
    {
        var roles = RoleAccounts(lines, AccountingPostingRoles.SalesRevenue);
        if (roles.Length == 1) return roles[0];
        if (roles.Length > 1)
            throw new ConflictException("sales_return_source_revenue_account_ambiguous", "قيد الفاتورة الأصلية يحتوي أكثر من حساب إيراد معرف بالدور المحاسبي.");
        if (!hasTax)
        {
            var legacy = lines.Where(x => x.SourceDocumentLineId is null && x.CreditAmount > 0m && x.AccountId != customerAccount)
                .Select(x => x.AccountId).Distinct().ToArray();
            if (legacy.Length == 1) return legacy[0];
        }
        throw LegacyRevenueReview();
    }

    /// <summary>
    /// Uses the immutable source invoice's posted base amounts to identify a single
    /// historical revenue header credit. Never chooses by descriptions/current settings.
    /// Ambiguous or split credits still require review.
    /// </summary>
    public static Guid ResolveRevenueAccount(
        IReadOnlyList<JournalEntryLine> lines, Guid customerAccount, bool hasTax,
        decimal sourceInvoiceBaseNet, decimal sourceInvoiceBaseTax)
    {
        var roles = RoleAccounts(lines, AccountingPostingRoles.SalesRevenue);
        if (roles.Length == 1) return roles[0];
        if (roles.Length > 1)
            throw new ConflictException("sales_return_source_revenue_account_ambiguous", "قيد الفاتورة الأصلية يحتوي أكثر من حساب إيراد معرف بالدور المحاسبي.");

        if (sourceInvoiceBaseNet <= 0m || sourceInvoiceBaseTax < 0m ||
            (hasTax && sourceInvoiceBaseTax <= 0m))
            throw LegacyRevenueReview();

        var headerCredits = lines.Where(x => x.SourceDocumentLineId is null && x.AccountId != customerAccount &&
            x.DebitAmount == 0m && x.CreditAmount > 0m).ToArray();
        // A legacy taxable invoice is reconstructed ONLY from the usual two-credit header:
        // one revenue credit and one tax credit. Unknown extra credits are not guessed.
        if (headerCredits.Length != (hasTax ? 2 : 1) ||
            !EqualBase(headerCredits.Sum(x => x.CreditAmount), sourceInvoiceBaseNet + sourceInvoiceBaseTax))
            throw LegacyRevenueReview();
        var candidates = headerCredits.Where(x => EqualBase(x.CreditAmount, sourceInvoiceBaseNet))
            .Select(x => x.AccountId).Distinct().ToArray();
        // An equal-sized tax credit also matches revenue and therefore cannot be inferred.
        if (candidates.Length == 1 && headerCredits.Count(x => EqualBase(x.CreditAmount, sourceInvoiceBaseNet)) == 1)
            return candidates[0];
        throw LegacyRevenueReview();
    }

    public static Guid ResolveTaxAccount(
        IReadOnlyList<JournalEntryLine> lines, Guid customerAccount, Guid revenueAccount,
        decimal sourceInvoiceBaseTax)
    {
        var roles = RoleAccounts(lines, AccountingPostingRoles.SalesTax);
        if (roles.Length == 1 && roles[0] != revenueAccount && roles[0] != customerAccount) return roles[0];
        if (roles.Length > 0)
            throw new ConflictException("sales_return_legacy_tax_role_review_required", "قيد الضريبة التاريخي متعدد أو متعارض ويستلزم المراجعة.");
        if (sourceInvoiceBaseTax <= 0m)
            throw new ConflictException("sales_return_legacy_tax_role_review_required", "مبلغ الضريبة التاريخي غير صالح.");

        var candidates = lines.Where(x => x.SourceDocumentLineId is null && x.DebitAmount == 0m &&
                                           x.AccountId != customerAccount && x.AccountId != revenueAccount &&
                                           x.CreditAmount > 0m && EqualBase(x.CreditAmount, sourceInvoiceBaseTax))
            .Select(x => x.AccountId).Distinct().ToArray();
        if (candidates.Length == 1) return candidates[0];
        throw new ConflictException("sales_return_legacy_tax_role_review_required",
            "تعذر تحديد حساب الضريبة التاريخي بشكل فريد؛ يلزم مراجعة القيد الأصلي.");
    }


    // Non-throwing inspection routines for normal pre-validation. Posting still
    // uses the guarded throwing methods under its transaction.
    public static bool TryResolveCustomerAccount(IReadOnlyList<JournalEntryLine> lines,
        Guid customerId, out Guid account)
    {
        var byRole = RoleAccounts(lines, AccountingPostingRoles.SalesCustomerReceivable);
        if (byRole.Length == 1) { account = byRole[0]; return true; }
        if (byRole.Length > 1) { account = Guid.Empty; return false; }
        var legacy = lines.Where(x => x.CustomerId == customerId && x.DebitAmount > 0m &&
                         x.SourceDocumentLineId == null).Select(x => x.AccountId).Distinct().ToArray();
        account = legacy.Length == 1 ? legacy[0] : Guid.Empty;
        return account != Guid.Empty;
    }

    public static bool TryResolveRevenueAccount(IReadOnlyList<JournalEntryLine> lines,
        Guid customerAccount, decimal invoiceNet, decimal invoiceTax, out Guid account)
    {
        var byRole = RoleAccounts(lines, AccountingPostingRoles.SalesRevenue);
        if (byRole.Length == 1) { account = byRole[0]; return true; }
        account = Guid.Empty;
        if (byRole.Length > 1 || invoiceNet <= 0m || invoiceTax < 0m) return false;
        var credits = lines.Where(x => x.SourceDocumentLineId == null &&
                         x.AccountId != customerAccount && x.DebitAmount == 0m &&
                         x.CreditAmount > 0m).ToArray();
        var requiredCount = invoiceTax > 0m ? 2 : 1;
        if (credits.Length != requiredCount ||
            !EqualBase(credits.Sum(x => x.CreditAmount), invoiceNet + invoiceTax)) return false;
        var matches = credits.Where(x => EqualBase(x.CreditAmount, invoiceNet)).ToArray();
        if (matches.Length != 1) return false;
        account = matches[0].AccountId;
        return account != Guid.Empty;
    }

    public static bool TryResolveTaxAccount(IReadOnlyList<JournalEntryLine> lines,
        Guid customerAccount, Guid revenueAccount, decimal invoiceTax, out Guid account)
    {
        account = Guid.Empty;
        var byRole = RoleAccounts(lines, AccountingPostingRoles.SalesTax);
        if (byRole.Length == 1 && byRole[0] != customerAccount && byRole[0] != revenueAccount)
        { account = byRole[0]; return true; }
        if (byRole.Length > 0 || invoiceTax <= 0m) return false;
        var credits = lines.Where(x => x.SourceDocumentLineId == null &&
            x.DebitAmount == 0m && x.AccountId != customerAccount &&
            x.AccountId != revenueAccount && x.CreditAmount > 0m &&
            EqualBase(x.CreditAmount, invoiceTax)).ToArray();
        if (credits.Length != 1) return false;
        account = credits[0].AccountId;
        return account != Guid.Empty;
    }

    public static bool TryResolveSourceLineAccount(IReadOnlyList<JournalEntryLine> lines,
        Guid sourceInvoiceLineId, string role, bool creditDirection, out Guid account)
    {
        account = Guid.Empty;
        var byRole = lines.Where(x => x.SourceDocumentLineId == sourceInvoiceLineId &&
            string.Equals(x.PostingRole, role, StringComparison.Ordinal))
            .Select(x => x.AccountId).Distinct().ToArray();
        if (byRole.Length == 1) { account = byRole[0]; return true; }
        if (byRole.Length > 1) return false;
        var legacy = lines.Where(x => x.SourceDocumentLineId == sourceInvoiceLineId &&
            (creditDirection ? x.CreditAmount > 0m : x.DebitAmount > 0m))
            .Select(x => x.AccountId).Distinct().ToArray();
        if (legacy.Length != 1) return false;
        account = legacy[0];
        return account != Guid.Empty;
    }

    private static Guid[] RoleAccounts(IReadOnlyList<JournalEntryLine> lines, string role) =>
        lines.Where(x => string.Equals(x.PostingRole, role, StringComparison.Ordinal))
            .Select(x => x.AccountId).Distinct().ToArray();

    private static bool EqualBase(decimal left, decimal right) => Math.Abs(left - right) <= 0.0001m;

    private static ConflictException LegacyRevenueReview() => new(
        "sales_return_legacy_revenue_role_review_required",
        "تعذر إثبات حساب إيراد الفاتورة التاريخي. راجع قيد الفاتورة الأصلي قبل الترحيل.");

    public static Guid ResolveRequiredRoleAccount(IReadOnlyList<JournalEntryLine> lines, string role, string code, string message)
    {
        var accounts = RoleAccounts(lines, role);
        if (accounts.Length == 1) return accounts[0];
        throw new ConflictException(code, message);
    }

    public static Guid ResolveSourceLineAccount(
        IReadOnlyList<JournalEntryLine> lines, Guid sourceInvoiceLineId, string role,
        bool creditDirection, string code, string message)
    {
        var roleAccounts = lines.Where(x => x.SourceDocumentLineId == sourceInvoiceLineId &&
            x.PostingRole == role).Select(x => x.AccountId).Distinct().ToArray();
        if (roleAccounts.Length == 1) return roleAccounts[0];
        if (roleAccounts.Length > 1) throw new ConflictException(code, message);

        var legacy = lines.Where(x => x.SourceDocumentLineId == sourceInvoiceLineId &&
            (creditDirection ? x.CreditAmount > 0m : x.DebitAmount > 0m))
            .Select(x => x.AccountId).Distinct().ToArray();
        if (legacy.Length == 1) return legacy[0];
        throw new ConflictException(code, message);
    }
}
