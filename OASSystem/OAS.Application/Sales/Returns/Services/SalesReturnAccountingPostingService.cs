using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Returns.Services;

public sealed class SalesReturnAccountingPostingService(
    IRepository<JournalEntry, Guid> journals,
    IReadRepository<JournalEntryLine, Guid> journalLines,
    IReadRepository<SalesInvoice, Guid> invoices,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Account, Guid> accounts,
    ISalesPostingPeriodService postingPeriods,
    ISequenceNumberGenerator sequences) : ISalesReturnAccountingPostingService
{
    public async Task<Guid> PostAsync(SalesReturn salesReturn, Guid postedBy, DateTime postedAtUtc, CancellationToken cancellationToken = default)
    {
        var existing = await journals.ListAsync(new Specification<JournalEntry>().Where(x =>
            x.SourceModule == SalesSourceReferences.Module &&
            x.SourceDocumentType == SalesSourceReferences.SalesReturn &&
            x.SourceDocumentId == salesReturn.Id &&
            (x.Status == JournalEntryStatus.Posted || x.Status == JournalEntryStatus.Reversed)), cancellationToken);
        if (existing.Count > 1)
            throw new ConflictException("sales_return_journal_duplicate", "يوجد أكثر من قيد محاسبي لمرتجع المبيعات.");
        if (existing.Count == 1) return existing[0].Id;

        var requiresInventory = salesReturn.Lines.Any(x => x.IsActive && x.RequiresInventory);
        var period = await postingPeriods.GetOpenPostingPeriodAsync(salesReturn.PostingDate, requiresInventory, cancellationToken);
        var customer = await customers.GetByIdAsync(salesReturn.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), salesReturn.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException("sales_return_customer_inactive", "العميل غير فعال ولا يمكن ترحيل المرتجع.");

        var sourceInvoice = await invoices.GetByIdAsync(salesReturn.SalesInvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesInvoice), salesReturn.SalesInvoiceId);
        if (sourceInvoice.Status != OAS.Domain.Sales.Enums.SalesInvoiceStatus.Posted || !sourceInvoice.JournalEntryId.HasValue)
            throw new ConflictException("sales_return_source_journal_missing", "لا يمكن ترحيل المرتجع لأن قيد الفاتورة الأصلية غير متاح.");

        var sourceJournalLines = await journalLines.ListAsync(
            new Specification<JournalEntryLine>().Where(x => x.JournalEntryId == sourceInvoice.JournalEntryId.Value),
            cancellationToken);
        if (sourceJournalLines.Count == 0)
            throw new ConflictException("sales_return_source_journal_lines_missing", "لا توجد أسطر قيد محفوظة للفاتورة الأصلية.");

        // A return must reverse the exact accounts used by the original posted invoice. Posting
        // profile or customer-account configuration may have changed since the sale.
        var customerAccount = ResolveSingleAccount(
            sourceJournalLines.Where(x => x.CustomerId == salesReturn.CustomerId && x.DebitAmount > 0m),
            "sales_return_source_customer_account_missing",
            "تعذر تحديد حساب العميل من قيد الفاتورة الأصلية.");
        var revenueAccount = ResolveSingleAccount(
            sourceJournalLines.Where(x => x.Description?.StartsWith("Sales revenue /", StringComparison.OrdinalIgnoreCase) == true),
            "sales_return_source_revenue_account_missing",
            "تعذر تحديد حساب إيراد المبيعات من قيد الفاتورة الأصلية.");
        Guid? taxAccount = salesReturn.TaxAmount > 0m
            ? ResolveSingleAccount(
                sourceJournalLines.Where(x => x.Description?.StartsWith("Sales tax /", StringComparison.OrdinalIgnoreCase) == true),
                "sales_return_source_tax_account_missing",
                "تعذر تحديد حساب ضريبة المبيعات من قيد الفاتورة الأصلية.")
            : null;

        await EnsureAccountAsync(customerAccount, cancellationToken);
        await EnsureAccountAsync(revenueAccount, cancellationToken);
        if (taxAccount.HasValue) await EnsureAccountAsync(taxAccount.Value, cancellationToken);

        var sequence = await sequences.NextAsync($"JournalEntry-{salesReturn.PostingDate.Year}", cancellationToken);
        var journal = JournalEntry.Create(
            Guid.NewGuid(),
            $"JV-{salesReturn.PostingDate.Year:0000}-{sequence:000000}",
            JournalType.Automatic,
            salesReturn.PostingDate,
            salesReturn.ReturnDate,
            period.Id,
            $"Sales return {salesReturn.ReturnCode}",
            SalesSourceReferences.Module,
            SalesSourceReferences.SalesReturn,
            salesReturn.Id,
            JournalEntryStatus.Draft);
        journal.SetBaseCurrencySnapshot(salesReturn.BaseCurrencyId, salesReturn.BaseCurrencyCodeSnapshot, salesReturn.BaseCurrencyDecimalPlacesSnapshot);

        var lineNo = 1;
        journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo++, customerAccount, 0m, salesReturn.BaseTotalAmount,
            0m, salesReturn.TotalAmount, salesReturn, $"Customer refund / {salesReturn.ReturnCode}", customer.Id));
        journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo++, revenueAccount, salesReturn.BaseNetAmount, 0m,
            salesReturn.NetAmount, 0m, salesReturn, $"Sales revenue reversal / {salesReturn.ReturnCode}", null));
        if (salesReturn.TaxAmount > 0m && taxAccount.HasValue)
        {
            journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo++, taxAccount.Value, salesReturn.BaseTaxAmount, 0m,
                salesReturn.TaxAmount, 0m, salesReturn, $"Sales tax reversal / {salesReturn.ReturnCode}", null));
        }

        foreach (var returnLine in salesReturn.Lines.Where(x => x.IsActive && x.RequiresInventory).OrderBy(x => x.LineNumber))
        {
            var cost = returnLine.TotalCostSnapshot ?? 0m;
            if (cost <= 0m) continue;

            var inventoryAccount = ResolveSingleAccount(
                sourceJournalLines.Where(x =>
                    x.SourceDocumentLineId == returnLine.SalesInvoiceLineId &&
                    x.Description?.StartsWith("Inventory /", StringComparison.OrdinalIgnoreCase) == true),
                "sales_return_source_inventory_account_missing",
                $"تعذر تحديد حساب المخزون من قيد الفاتورة الأصلية للسطر {returnLine.LineNumber}.");
            var cogsAccount = ResolveSingleAccount(
                sourceJournalLines.Where(x =>
                    x.SourceDocumentLineId == returnLine.SalesInvoiceLineId &&
                    x.Description?.StartsWith("COGS /", StringComparison.OrdinalIgnoreCase) == true),
                "sales_return_source_cogs_account_missing",
                $"تعذر تحديد حساب تكلفة المبيعات من قيد الفاتورة الأصلية للسطر {returnLine.LineNumber}.");
            await EnsureAccountAsync(inventoryAccount, cancellationToken);
            await EnsureAccountAsync(cogsAccount, cancellationToken);

            journal.AddLine(CreateBaseCurrencyLine(journal.Id, lineNo++, inventoryAccount, cost, 0m, salesReturn,
                $"Inventory return / {salesReturn.ReturnCode} / line {returnLine.LineNumber}", returnLine.ProductVariantId, returnLine.WarehouseId, returnLine.Id));
            journal.AddLine(CreateBaseCurrencyLine(journal.Id, lineNo++, cogsAccount, 0m, cost, salesReturn,
                $"COGS reversal / {salesReturn.ReturnCode} / line {returnLine.LineNumber}", returnLine.ProductVariantId, returnLine.WarehouseId, returnLine.Id));
        }

        if (!journal.IsBalanced())
            throw new ConflictException("sales_return_journal_unbalanced", "تعذر ترحيل مرتجع المبيعات لأن القيد المحاسبي غير متوازن.");
        journal.SetPendingApproval();
        journal.Approve(postedBy, postedAtUtc);
        journal.Post(postedBy, postedAtUtc);
        await journals.AddAsync(journal, cancellationToken);
        return journal.Id;
    }

    private static Guid ResolveSingleAccount(IEnumerable<JournalEntryLine> candidates, string code, string message)
    {
        var accounts = candidates.Select(x => x.AccountId).Distinct().ToArray();
        if (accounts.Length != 1)
            throw new ConflictException(code, message);
        return accounts[0];
    }

    private async Task EnsureAccountAsync(Guid accountId, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(accountId, ct)
            ?? throw new ConflictException("sales_return_account_missing", "الحساب المحاسبي المستخدم في الفاتورة الأصلية لم يعد موجودًا.");
        if (!account.CanReceivePosting())
            throw new ConflictException("sales_return_account_invalid", $"الحساب '{account.Code}' المستخدم في الفاتورة الأصلية غير صالح حاليًا للترحيل.");
    }

    private static JournalEntryLine CreateInvoiceCurrencyLine(
        Guid journalId, int lineNo, Guid accountId, decimal debitBase, decimal creditBase,
        decimal debitTx, decimal creditTx, SalesReturn salesReturn, string description, Guid? customerId) =>
        JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journalId, lineNo, accountId, debitBase, creditBase,
            salesReturn.CurrencyId, salesReturn.CurrencyCodeSnapshot, salesReturn.CurrencyDecimalPlacesSnapshot,
            debitTx, creditTx, salesReturn.ExchangeRate, salesReturn.ExchangeRateDate,
            salesReturn.ExchangeRateType, salesReturn.ExchangeRateSource, description,
            customerId, null, null, null, null, null, null, null);

    private static JournalEntryLine CreateBaseCurrencyLine(
        Guid journalId, int lineNo, Guid accountId, decimal debit, decimal credit, SalesReturn salesReturn,
        string description, Guid? productVariantId, Guid? warehouseId, Guid? sourceLineId) =>
        JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journalId, lineNo, accountId, debit, credit,
            salesReturn.BaseCurrencyId, salesReturn.BaseCurrencyCodeSnapshot, salesReturn.BaseCurrencyDecimalPlacesSnapshot,
            debit, credit, 1m, salesReturn.PostingDate, ExchangeRateType.Accounting, ExchangeRateSource.System,
            description, null, null, null, null, null, productVariantId, warehouseId, sourceLineId);
}
