using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Services;

public sealed class PurchasingAccountingPort(
    OasDbContext dbContext,
    ISequenceNumberGenerator sequenceNumberGenerator,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IPurchasingAccountingPort
{
    private const string PurchasingModule = "Purchasing";
    private const string ReceiptDocumentType = "PurchaseReceipt";
    private const string InvoiceDocumentType = "PurchaseInvoice";
    private const string ReturnDocumentType = "PurchaseReturn";
    private const string InventoryRole = "Inventory";
    private const string GrniRole = "GoodsReceivedNotInvoiced";
    private const string PurchaseTaxRole = "PurchaseTax";
    private const string PriceVarianceRole = "PurchasePriceVariance";

    public async Task ValidatePostingPeriodAsync(DateOnly postingDate, CancellationToken cancellationToken = default)
        => _ = await ResolveFiscalPeriodAsync(postingDate, cancellationToken);

    public async Task ValidateSupplierAccountAsync(Guid supplierId, CancellationToken cancellationToken = default)
        => _ = await ResolveSupplierAccountAsync(supplierId, cancellationToken);

    public async Task<PurchasingAccountingPostingResult> PostPurchaseReceiptJournalAsync(PurchasingReceiptAccountingContext context, CancellationToken cancellationToken = default)
    {
        var existing = await FindExistingJournalAsync(ReceiptDocumentType, context.PurchaseReceiptId, cancellationToken);
        if (existing is not null) return new PurchasingAccountingPostingResult(existing.Id);
        if (context.Lines.Count == 0) throw new ConflictException("purchasing_receipt_no_accounting_lines", "لا توجد بنود مقبولة لإنشاء قيد الاستلام.");

        var period = await ResolveFiscalPeriodAsync(context.PostingDate, cancellationToken);
        var profile = await ResolveProfileAsync(ReceiptDocumentType, cancellationToken);
        var inventoryAccount = await ResolveProfileAccountAsync(profile, InventoryRole, cancellationToken);
        var grniAccount = await ResolveProfileAccountAsync(profile, GrniRole, cancellationToken);
        var supplier = await dbContext.Set<Supplier>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == context.SupplierId, cancellationToken)
            ?? throw new NotFoundException(nameof(Supplier), context.SupplierId);
        if (!supplier.IsActive) throw new ConflictException("purchasing_supplier_inactive", "المورد غير فعال.");
        var (settings, baseCurrency) = await ResolveBaseCurrencyAsync(cancellationToken);
        var userGuid = ResolveUserGuid();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var journal = await CreateJournalAsync(context.PostingDate, period.Id, $"Purchase receipt {context.ReceiptCode}", ReceiptDocumentType,
            context.PurchaseReceiptId, baseCurrency, cancellationToken);

        var lineNo = 1;
        var total = 0m;
        foreach (var source in context.Lines.Where(x => x.BaseAmount > 0))
        {
            var amount = Math.Round(source.BaseAmount, 4);
            total += amount;
            journal.AddLine(CreateBaseCurrencyLine(journal.Id, lineNo++, inventoryAccount.Id, amount, 0m, baseCurrency,
                context.PostingDate, $"Inventory receipt {context.ReceiptCode}", null, source.ProductVariantId, source.PurchaseReceiptLineId));
        }
        if (total <= 0) throw new ConflictException("purchasing_receipt_value_invalid", "قيمة الاستلام المحاسبية يجب أن تكون أكبر من صفر.");
        journal.AddLine(CreateBaseCurrencyLine(journal.Id, lineNo, grniAccount.Id, 0m, total, baseCurrency,
            context.PostingDate, $"GRNI {context.ReceiptCode}", supplier.Id, null, null));

        CompleteJournal(journal, userGuid, now);
        await dbContext.Set<JournalEntry>().AddAsync(journal, cancellationToken);
        return new PurchasingAccountingPostingResult(journal.Id);
    }

    public async Task<PurchasingAccountingPostingResult> PostPurchaseInvoiceJournalAsync(PurchasingInvoiceAccountingContext context, CancellationToken cancellationToken = default)
    {
        var existing = await FindExistingJournalAsync(InvoiceDocumentType, context.PurchaseInvoiceId, cancellationToken);
        if (existing is not null) return new PurchasingAccountingPostingResult(existing.Id);

        var period = await ResolveFiscalPeriodAsync(context.PostingDate, cancellationToken);
        var profile = await ResolveProfileAsync(InvoiceDocumentType, cancellationToken);
        var grniAccount = await ResolveProfileAccountAsync(profile, GrniRole, cancellationToken);
        Account? taxAccount = context.PurchaseTaxBaseAmount > 0 ? await ResolveProfileAccountAsync(profile, PurchaseTaxRole, cancellationToken) : null;
        Account? varianceAccount = context.PurchasePriceVarianceBaseAmount != 0 ? await ResolveProfileAccountAsync(profile, PriceVarianceRole, cancellationToken) : null;
        var (supplier, supplierAccount) = await ResolveSupplierAccountAsync(context.SupplierId, cancellationToken);
        var (settings, baseCurrency) = await ResolveBaseCurrencyAsync(cancellationToken);
        var transactionCurrency = await dbContext.Set<Currency>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == context.CurrencyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), context.CurrencyId);
        if (!transactionCurrency.IsActive) throw new ConflictException("purchasing_currency_inactive", "عملة فاتورة المورد غير فعالة.");
        if (context.ExchangeRate <= 0) throw new ConflictException("purchasing_exchange_rate_invalid", "سعر الصرف يجب أن يكون أكبر من صفر.");

        var userGuid = ResolveUserGuid();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var journal = await CreateJournalAsync(context.PostingDate, period.Id, $"Purchase invoice {context.PurchaseInvoiceCode}", InvoiceDocumentType,
            context.PurchaseInvoiceId, baseCurrency, cancellationToken);
        var lineNo = 1;

        if (context.GrniBaseAmount > 0)
            journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo++, grniAccount.Id, context.GrniBaseAmount, 0m, transactionCurrency,
                context.ExchangeRate, context.PostingDate, $"GRNI clearance {context.PurchaseInvoiceCode}", supplier.Id));
        if (context.PurchaseTaxBaseAmount > 0 && taxAccount is not null)
            journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo++, taxAccount.Id, context.PurchaseTaxBaseAmount, 0m, transactionCurrency,
                context.ExchangeRate, context.PostingDate, $"Purchase tax {context.PurchaseInvoiceCode}", supplier.Id));
        if (context.PurchasePriceVarianceBaseAmount > 0 && varianceAccount is not null)
            journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo++, varianceAccount.Id, context.PurchasePriceVarianceBaseAmount, 0m, transactionCurrency,
                context.ExchangeRate, context.PostingDate, $"Purchase price variance {context.PurchaseInvoiceCode}", supplier.Id));
        else if (context.PurchasePriceVarianceBaseAmount < 0 && varianceAccount is not null)
            journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo++, varianceAccount.Id, 0m, Math.Abs(context.PurchasePriceVarianceBaseAmount), transactionCurrency,
                context.ExchangeRate, context.PostingDate, $"Purchase price variance {context.PurchaseInvoiceCode}", supplier.Id));

        if (context.SupplierPayableBaseAmount <= 0) throw new ConflictException("purchasing_supplier_payable_invalid", "قيمة مديونية المورد يجب أن تكون أكبر من صفر.");
        journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo, supplierAccount.Id, 0m, context.SupplierPayableBaseAmount, transactionCurrency,
            context.ExchangeRate, context.PostingDate, $"Supplier payable {context.PurchaseInvoiceCode}", supplier.Id));

        if (!journal.IsBalanced())
            throw new ConflictException("purchasing_journal_unbalanced", "تعذر ترحيل فاتورة المورد لأن القيد المحاسبي غير متوازن.");
        CompleteJournal(journal, userGuid, now);
        await dbContext.Set<JournalEntry>().AddAsync(journal, cancellationToken);
        return new PurchasingAccountingPostingResult(journal.Id);
    }

    public async Task<PurchasingAccountingPostingResult> PostPurchaseReturnJournalAsync(
        PurchasingReturnAccountingContext context,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindExistingJournalAsync(ReturnDocumentType, context.PurchaseReturnId, cancellationToken);
        if (existing is not null) return new PurchasingAccountingPostingResult(existing.Id);
        if (context.InventoryCostBaseAmount < 0 || context.ReceiptCostBaseAmount < 0 ||
            context.SupplierNetBaseAmount < 0 || context.SupplierTaxBaseAmount < 0 || context.SupplierGrossBaseAmount < 0)
            throw new ConflictException("purchase_return_amount_invalid", "قيم مرتجع المشتريات المحاسبية غير صالحة.");

        var period = await ResolveFiscalPeriodAsync(context.PostingDate, cancellationToken);
        var receiptProfile = await ResolveProfileAsync(ReceiptDocumentType, cancellationToken);
        var inventoryAccount = await ResolveProfileAccountAsync(receiptProfile, InventoryRole, cancellationToken);
        var (_, baseCurrency) = await ResolveBaseCurrencyAsync(cancellationToken);
        var (supplier, supplierAccount) = await ResolveSupplierAccountAsync(context.SupplierId, cancellationToken);
        var userGuid = ResolveUserGuid();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var journal = await CreateJournalAsync(context.PostingDate, period.Id, $"Purchase return {context.ReturnCode}", ReturnDocumentType,
            context.PurchaseReturnId, baseCurrency, cancellationToken);
        var lineNo = 1;

        if (context.PurchaseInvoiceId.HasValue)
        {
            var invoice = await dbContext.Set<OAS.Domain.Purchasing.Entities.PurchaseInvoice>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == context.PurchaseInvoiceId.Value, cancellationToken)
                ?? throw new NotFoundException("PurchaseInvoice", context.PurchaseInvoiceId.Value);
            if (invoice.SupplierId != context.SupplierId || invoice.Status != OAS.Domain.Purchasing.Enums.PurchaseInvoiceStatus.Posted)
                throw new ConflictException("purchase_return_invoice_invalid", "فاتورة المورد المرتبطة بالمرتجع غير صالحة للترحيل العكسي.");
            var invoiceProfile = await ResolveProfileAsync(InvoiceDocumentType, cancellationToken);
            Account? taxAccount = context.SupplierTaxBaseAmount > 0m
                ? await ResolveProfileAccountAsync(invoiceProfile, PurchaseTaxRole, cancellationToken)
                : null;
            Account? varianceAccount = context.PurchasePriceVarianceBaseAmount != 0m
                ? await ResolveProfileAccountAsync(invoiceProfile, PriceVarianceRole, cancellationToken)
                : null;
            var txCurrency = await dbContext.Set<Currency>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == invoice.CurrencyId, cancellationToken)
                ?? throw new NotFoundException(nameof(Currency), invoice.CurrencyId);

            if (context.SupplierGrossBaseAmount <= 0m)
                throw new ConflictException("purchase_return_supplier_amount_invalid", "قيمة عكس مديونية المورد يجب أن تكون أكبر من صفر.");
            journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo++, supplierAccount.Id,
                context.SupplierGrossBaseAmount, 0m, txCurrency, invoice.ExchangeRate, context.PostingDate,
                $"Supplier return {context.ReturnCode}", supplier.Id));
            if (context.SupplierTaxBaseAmount > 0m && taxAccount is not null)
                journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo++, taxAccount.Id,
                    0m, context.SupplierTaxBaseAmount, txCurrency, invoice.ExchangeRate, context.PostingDate,
                    $"Purchase tax reversal {context.ReturnCode}", supplier.Id));
            if (context.InventoryCostBaseAmount > 0m)
                journal.AddLine(CreateBaseCurrencyLine(journal.Id, lineNo++, inventoryAccount.Id,
                    0m, context.InventoryCostBaseAmount, baseCurrency, context.PostingDate,
                    $"Inventory purchase return {context.ReturnCode}", supplier.Id, null, null));

            AddVarianceLine(journal, ref lineNo, varianceAccount, context.PurchasePriceVarianceBaseAmount,
                baseCurrency, context.PostingDate, context.ReturnCode, supplier.Id);
        }
        else
        {
            var grniAccount = await ResolveProfileAccountAsync(receiptProfile, GrniRole, cancellationToken);
            Account? varianceAccount = null;
            if (context.PurchasePriceVarianceBaseAmount != 0m)
            {
                var invoiceProfile = await ResolveProfileAsync(InvoiceDocumentType, cancellationToken);
                varianceAccount = await ResolveProfileAccountAsync(invoiceProfile, PriceVarianceRole, cancellationToken);
            }
            if (context.ReceiptCostBaseAmount <= 0m)
                throw new ConflictException("purchase_return_receipt_amount_invalid", "قيمة تكلفة الاستلام للمرتجع يجب أن تكون أكبر من صفر.");
            journal.AddLine(CreateBaseCurrencyLine(journal.Id, lineNo++, grniAccount.Id,
                context.ReceiptCostBaseAmount, 0m, baseCurrency, context.PostingDate,
                $"GRNI reversal {context.ReturnCode}", supplier.Id, null, null));
            journal.AddLine(CreateBaseCurrencyLine(journal.Id, lineNo++, inventoryAccount.Id,
                0m, context.InventoryCostBaseAmount, baseCurrency, context.PostingDate,
                $"Inventory purchase return {context.ReturnCode}", supplier.Id, null, null));
            AddVarianceLine(journal, ref lineNo, varianceAccount, context.PurchasePriceVarianceBaseAmount,
                baseCurrency, context.PostingDate, context.ReturnCode, supplier.Id);
        }

        if (!journal.IsBalanced())
            throw new ConflictException("purchase_return_journal_unbalanced", "تعذر ترحيل مرتجع المشتريات لأن القيد المحاسبي غير متوازن.");
        CompleteJournal(journal, userGuid, now);
        await dbContext.Set<JournalEntry>().AddAsync(journal, cancellationToken);
        return new PurchasingAccountingPostingResult(journal.Id);
    }

    private static void AddVarianceLine(
        JournalEntry journal,
        ref int lineNo,
        Account? varianceAccount,
        decimal variance,
        Currency baseCurrency,
        DateOnly postingDate,
        string returnCode,
        Guid supplierId)
    {
        if (variance == 0m) return;
        if (varianceAccount is null)
            throw new ConflictException("purchase_return_variance_account_missing", "حساب فرق سعر المشتريات مطلوب لترحيل المرتجع.");
        journal.AddLine(CreateBaseCurrencyLine(journal.Id, lineNo++, varianceAccount.Id,
            variance < 0m ? Math.Abs(variance) : 0m,
            variance > 0m ? variance : 0m,
            baseCurrency, postingDate, $"Purchase return variance {returnCode}", supplierId, null, null));
    }

    private async Task<JournalEntry?> FindExistingJournalAsync(string documentType, Guid sourceId, CancellationToken ct) =>
        await dbContext.Set<JournalEntry>().AsNoTracking().FirstOrDefaultAsync(x => x.SourceModule == PurchasingModule && x.SourceDocumentType == documentType && x.SourceDocumentId == sourceId, ct);

    private async Task<FiscalPeriod> ResolveFiscalPeriodAsync(DateOnly postingDate, CancellationToken ct)
    {
        var periods = await dbContext.Set<FiscalPeriod>().AsNoTracking().Where(x => x.StartDate <= postingDate && x.EndDate >= postingDate).Take(2).ToListAsync(ct);
        if (periods.Count == 0) throw new ConflictException("fiscal_period_not_found", "لا توجد فترة مالية تغطي تاريخ الترحيل.");
        if (periods.Count > 1) throw new ConflictException("fiscal_period_overlap", "يوجد أكثر من فترة مالية تغطي تاريخ الترحيل.");
        if (!periods[0].CanPostAccounting()) throw new ConflictException("fiscal_period_closed", "الفترة المالية مغلقة.");
        return periods[0];
    }

    private async Task<PostingProfile> ResolveProfileAsync(string documentType, CancellationToken ct)
    {
        var profiles = await dbContext.Set<PostingProfile>().Include(x => x.Lines)
            .Where(x => x.IsActive && x.Module == PurchasingModule && x.DocumentType == documentType).Take(2).ToListAsync(ct);
        if (profiles.Count != 1)
            throw new ConflictException("purchasing_posting_profile_incomplete", "إعدادات ترحيل المشتريات غير مكتملة أو يوجد أكثر من ملف ترحيل فعال للمستند.");
        return profiles[0];
    }

    private async Task<Account> ResolveProfileAccountAsync(PostingProfile profile, string role, CancellationToken ct)
    {
        var matches = profile.Lines.Where(x => string.Equals(x.AccountRole, role, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1) throw new ConflictException("purchasing_posting_role_missing", $"إعدادات ترحيل المشتريات لا تحتوي دور الحساب '{role}' بشكل صحيح.");
        return await ResolvePostingAccountAsync(matches[0].AccountId, ct);
    }

    private async Task<(Supplier Supplier, Account Account)> ResolveSupplierAccountAsync(Guid supplierId, CancellationToken ct)
    {
        var supplier = await dbContext.Set<Supplier>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == supplierId, ct)
            ?? throw new NotFoundException(nameof(Supplier), supplierId);
        if (!supplier.IsActive) throw new ConflictException("purchasing_supplier_inactive", "المورد غير فعال.");
        if (supplier.AccountId == Guid.Empty) throw new ConflictException("purchasing_supplier_account_missing", "لا يوجد حساب محاسبي صالح للمورد.");
        var account = await ResolvePostingAccountAsync(supplier.AccountId, ct);
        if (account.AccountType != AccountType.Subledger)
            throw new ConflictException("purchasing_supplier_account_not_subledger", "حساب المورد يجب أن يكون حساب Subledger قابلًا للترحيل.");
        return (supplier, account);
    }

    private async Task<Account> ResolvePostingAccountAsync(Guid accountId, CancellationToken ct)
    {
        var account = await dbContext.Set<Account>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == accountId, ct)
            ?? throw new NotFoundException(nameof(Account), accountId);
        if (!account.CanReceivePosting()) throw new ConflictException("purchasing_account_not_postable", $"الحساب '{account.Code}' غير صالح للترحيل.");
        return account;
    }

    private async Task<(AccountingSettings Settings, Currency Currency)> ResolveBaseCurrencyAsync(CancellationToken ct)
    {
        var settings = await dbContext.Set<AccountingSettings>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == AccountingSettings.SingletonId, ct)
            ?? throw new ConflictException("accounting_settings_required", "يجب إعداد المحاسبة وتحديد العملة الأساسية أولًا.");
        var currency = await dbContext.Set<Currency>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == settings.BaseCurrencyId, ct)
            ?? throw new ConflictException("base_currency_missing", "العملة الأساسية المحددة في إعدادات المحاسبة غير موجودة.");
        return (settings, currency);
    }

    private async Task<JournalEntry> CreateJournalAsync(DateOnly postingDate, Guid periodId, string description, string documentType, Guid documentId, Currency baseCurrency, CancellationToken ct)
    {
        var sequence = await sequenceNumberGenerator.NextAsync($"JournalEntry-{postingDate.Year}", ct);
        var journal = JournalEntry.Create(Guid.NewGuid(), $"JV-{postingDate.Year:0000}-{sequence:000000}", JournalType.Automatic,
            postingDate, postingDate, periodId, description, PurchasingModule, documentType, documentId, JournalEntryStatus.Draft);
        journal.SetBaseCurrencySnapshot(baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces);
        return journal;
    }

    private static JournalEntryLine CreateBaseCurrencyLine(Guid journalId, int number, Guid accountId, decimal debit, decimal credit,
        Currency currency, DateOnly date, string description, Guid? supplierId, Guid? productVariantId, Guid? sourceLineId) =>
        JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), journalId, number, accountId, debit, credit, currency.Id, currency.Code,
            currency.DecimalPlaces, debit, credit, 1m, date, ExchangeRateType.Accounting, ExchangeRateSource.System, description,
            null, supplierId, null, null, null, productVariantId, null, sourceLineId);

    private static JournalEntryLine CreateInvoiceCurrencyLine(Guid journalId, int number, Guid accountId, decimal debit, decimal credit,
        Currency currency, decimal rate, DateOnly date, string description, Guid supplierId)
    {
        var txDebit = debit > 0 ? Math.Round(debit / rate, currency.DecimalPlaces) : 0m;
        var txCredit = credit > 0 ? Math.Round(credit / rate, currency.DecimalPlaces) : 0m;
        return JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), journalId, number, accountId, debit, credit, currency.Id, currency.Code,
            currency.DecimalPlaces, txDebit, txCredit, rate, date, ExchangeRateType.Accounting, ExchangeRateSource.System, description,
            null, supplierId, null, null, null, null, null, null);
    }

    private Guid ResolveUserGuid()
    {
        if (Guid.TryParse(currentUser.UserId, out var userId) && userId != Guid.Empty) return userId;
        throw new ConflictException("purchasing_posting_user_invalid", "تعذر تحديد المستخدم المسؤول عن الترحيل المحاسبي.");
    }

    private static void CompleteJournal(JournalEntry journal, Guid userId, DateTime nowUtc)
    {
        journal.SetPendingApproval();
        journal.Approve(userId, nowUtc);
        journal.Post(userId, nowUtc);
    }
}
