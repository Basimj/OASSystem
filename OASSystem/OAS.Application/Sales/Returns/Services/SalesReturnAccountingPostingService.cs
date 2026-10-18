using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Accounting;
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
            x.SourceDocumentId == salesReturn.Id), cancellationToken);
        if (existing.Count > 1)
            throw new ConflictException("sales_return_journal_duplicate", "يوجد أكثر من قيد للمرتجع؛ يلزم المراجعة.");
        if (existing.Count == 1)
        {
            var recorded = existing[0];
            if (recorded.Status is JournalEntryStatus.Posted or JournalEntryStatus.Reversed) return recorded.Id;
            throw new ConflictException("sales_return_journal_partial_posting", "توجد محاولة ترحيل غير مكتملة؛ يلزم مراجعة القيد قبل الإعادة.");
        }

        var restoresInventory = salesReturn.Lines.Any(x => x.IsActive && x.RestoresInventoryAsset);
        var period = await postingPeriods.GetOpenPostingPeriodAsync(salesReturn.PostingDate, restoresInventory, cancellationToken);
        var customer = await customers.GetByIdAsync(salesReturn.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), salesReturn.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException("sales_return_customer_inactive", "العميل غير فعال ولا يمكن ترحيل المرتجع.");

        var invoice = await invoices.GetByIdAsync(salesReturn.SalesInvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesInvoice), salesReturn.SalesInvoiceId);
        if (invoice.Status != OAS.Domain.Sales.Enums.SalesInvoiceStatus.Posted || !invoice.JournalEntryId.HasValue)
            throw new ConflictException("sales_return_source_journal_missing", "قيد الفاتورة الأصلية غير متاح.");

        var sourceLines = await journalLines.ListAsync(new Specification<JournalEntryLine>()
            .Where(x => x.JournalEntryId == invoice.JournalEntryId.Value), cancellationToken);
        if (sourceLines.Count == 0)
            throw new ConflictException("sales_return_source_journal_lines_missing", "لا توجد أسطر قيد للفاتورة الأصلية.");

        // Only roles or verified historical invoice amounts. Never use descriptions or a new profile.
        var customerAccount = SalesReturnHistoricalAccountResolver.ResolveCustomerAccount(sourceLines, salesReturn.CustomerId);
        var revenueAccount = SalesReturnHistoricalAccountResolver.ResolveRevenueAccount(sourceLines,
            customerAccount, invoice.BaseTaxAmount > 0m,
            invoice.BaseTotalAmount - invoice.BaseTaxAmount, invoice.BaseTaxAmount);
        Guid? taxAccount = salesReturn.TaxAmount > 0m
            ? SalesReturnHistoricalAccountResolver.ResolveTaxAccount(sourceLines, customerAccount, revenueAccount, invoice.BaseTaxAmount)
            : null;

        await EnsureHistoricalAccountAsync(customerAccount, cancellationToken);
        await EnsureHistoricalAccountAsync(revenueAccount, cancellationToken);
        if (taxAccount.HasValue) await EnsureHistoricalAccountAsync(taxAccount.Value, cancellationToken);

        // Validate all inventory accounts before creating a new journal.
        var costAccounts = new Dictionary<Guid, (Guid Inventory, Guid Cogs)>();
        foreach (var line in salesReturn.Lines.Where(x => x.IsActive && x.RestoresInventoryAsset && (x.TotalCostSnapshot ?? 0m) > 0m))
        {
            var inventoryAccount = SalesReturnHistoricalAccountResolver.ResolveSourceLineAccount(sourceLines,
                line.SalesInvoiceLineId, AccountingPostingRoles.SalesInventory, true,
                "sales_return_source_inventory_account_missing", $"حساب المخزون التاريخي غير معروف للسطر {line.LineNumber}.");
            var cogsAccount = SalesReturnHistoricalAccountResolver.ResolveSourceLineAccount(sourceLines,
                line.SalesInvoiceLineId, AccountingPostingRoles.SalesCOGS, false,
                "sales_return_source_cogs_account_missing", $"حساب تكلفة المبيعات التاريخي غير معروف للسطر {line.LineNumber}.");
            await EnsureHistoricalAccountAsync(inventoryAccount, cancellationToken);
            await EnsureHistoricalAccountAsync(cogsAccount, cancellationToken);
            costAccounts[line.Id] = (inventoryAccount, cogsAccount);
        }

        var sequence = await sequences.NextAsync($"JournalEntry-{salesReturn.PostingDate.Year}", cancellationToken);
        var journal = JournalEntry.Create(Guid.NewGuid(),
            $"JV-{salesReturn.PostingDate.Year:0000}-{sequence:000000}",
            JournalType.Automatic, salesReturn.PostingDate, salesReturn.ReturnDate, period.Id,
            $"Sales return {salesReturn.ReturnCode}", SalesSourceReferences.Module,
            SalesSourceReferences.SalesReturn, salesReturn.Id, JournalEntryStatus.Draft);
        journal.SetBaseCurrencySnapshot(salesReturn.BaseCurrencyId,
            salesReturn.BaseCurrencyCodeSnapshot, salesReturn.BaseCurrencyDecimalPlacesSnapshot);

        var number = 1;
        journal.AddLine(InvoiceLine(journal.Id, number++, customerAccount, 0m, salesReturn.BaseTotalAmount,
            0m, salesReturn.TotalAmount, salesReturn, $"Customer return / {salesReturn.ReturnCode}",
            customer.Id, AccountingPostingRoles.SalesCustomerReceivable));
        journal.AddLine(InvoiceLine(journal.Id, number++, revenueAccount, salesReturn.BaseNetAmount, 0m,
            salesReturn.NetAmount, 0m, salesReturn, $"Sales revenue reversal / {salesReturn.ReturnCode}",
            null, AccountingPostingRoles.SalesRevenue));
        if (taxAccount.HasValue)
            journal.AddLine(InvoiceLine(journal.Id, number++, taxAccount.Value, salesReturn.BaseTaxAmount, 0m,
                salesReturn.TaxAmount, 0m, salesReturn, $"Sales tax reversal / {salesReturn.ReturnCode}",
                null, AccountingPostingRoles.SalesTax));

        foreach (var line in salesReturn.Lines.Where(x => x.IsActive && x.RestoresInventoryAsset).OrderBy(x => x.LineNumber))
        {
            var cost = line.TotalCostSnapshot ?? 0m;
            if (cost <= 0m) continue;
            var account = costAccounts[line.Id];
            journal.AddLine(BaseLine(journal.Id, number++, account.Inventory, cost, 0m, salesReturn,
                $"Inventory return / {salesReturn.ReturnCode} / line {line.LineNumber}",
                line.ProductVariantId, line.WarehouseId, line.Id, AccountingPostingRoles.SalesInventory));
            journal.AddLine(BaseLine(journal.Id, number++, account.Cogs, 0m, cost, salesReturn,
                $"COGS reversal / {salesReturn.ReturnCode} / line {line.LineNumber}",
                line.ProductVariantId, line.WarehouseId, line.Id, AccountingPostingRoles.SalesCOGS));
        }
        if (!journal.IsBalanced())
            throw new ConflictException("sales_return_journal_unbalanced", "القيد المحاسبي للمرتجع غير متوازن.");
        journal.SetPendingApproval();
        journal.Approve(postedBy, postedAtUtc);
        journal.Post(postedBy, postedAtUtc);
        await journals.AddAsync(journal, cancellationToken);
        return journal.Id;
    }

    private async Task EnsureHistoricalAccountAsync(Guid id, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(id, ct)
            ?? throw new ConflictException("sales_return_account_missing", "الحساب التاريخي لم يعد موجودًا.");
        if (!account.IsPostingAccount || account.AccountType == AccountType.Header)
            throw new ConflictException("sales_return_account_invalid", $"الحساب '{account.Code}' ليس حساب ترحيل صالحًا.");
    }

    private static JournalEntryLine InvoiceLine(Guid journalId, int line, Guid accountId,
        decimal drBase, decimal crBase, decimal drTx, decimal crTx, SalesReturn sr,
        string description, Guid? customerId, string role) =>
        JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), journalId, line, accountId, drBase, crBase,
            sr.CurrencyId, sr.CurrencyCodeSnapshot, sr.CurrencyDecimalPlacesSnapshot, drTx, crTx,
            sr.ExchangeRate, sr.ExchangeRateDate, sr.ExchangeRateType, sr.ExchangeRateSource,
            description, customerId, null, null, null, null, null, null, null, role);

    private static JournalEntryLine BaseLine(Guid journalId, int line, Guid accountId,
        decimal dr, decimal cr, SalesReturn sr, string description,
        Guid? product, Guid? warehouse, Guid? sourceId, string role) =>
        JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), journalId, line, accountId, dr, cr,
            sr.BaseCurrencyId, sr.BaseCurrencyCodeSnapshot, sr.BaseCurrencyDecimalPlacesSnapshot,
            dr, cr, 1m, sr.PostingDate, ExchangeRateType.Accounting, ExchangeRateSource.System,
            description, null, null, null, null, null, product, warehouse, sourceId, role);
}
