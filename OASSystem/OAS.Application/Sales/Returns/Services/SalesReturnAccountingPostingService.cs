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
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<PostingProfile, Guid> profiles,
    IReadRepository<PostingProfileLine, Guid> profileLines,
    ISalesPostingPeriodService postingPeriods,
    ISequenceNumberGenerator sequences) : ISalesReturnAccountingPostingService
{
    private const string SalesRevenueRole = "SalesRevenue";
    private const string TaxPayableRole = "TaxPayable";
    private const string InventoryRole = "Inventory";
    private const string CogsRole = "COGS";

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
        await EnsureAccountAsync(customer.AccountId, cancellationToken);

        var profile = await ResolveInvoiceProfileAsync(cancellationToken);
        var configured = await profileLines.ListAsync(new Specification<PostingProfileLine>()
            .Where(x => x.PostingProfileId == profile.Id), cancellationToken);
        var revenueAccount = await RequiredRoleAsync(configured, SalesRevenueRole, cancellationToken);
        Guid? taxAccount = salesReturn.TaxAmount > 0m ? await RequiredRoleAsync(configured, TaxPayableRole, cancellationToken) : null;
        Guid? inventoryAccount = requiresInventory ? await RequiredRoleAsync(configured, InventoryRole, cancellationToken) : null;
        Guid? cogsAccount = requiresInventory ? await RequiredRoleAsync(configured, CogsRole, cancellationToken) : null;

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
        journal.AddLine(CreateInvoiceCurrencyLine(journal.Id, lineNo++, customer.AccountId, 0m, salesReturn.BaseTotalAmount,
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
            journal.AddLine(CreateBaseCurrencyLine(journal.Id, lineNo++, inventoryAccount!.Value, cost, 0m, salesReturn,
                $"Inventory return / {salesReturn.ReturnCode} / line {returnLine.LineNumber}", returnLine.ProductVariantId, returnLine.WarehouseId, returnLine.Id));
            journal.AddLine(CreateBaseCurrencyLine(journal.Id, lineNo++, cogsAccount!.Value, 0m, cost, salesReturn,
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

    private async Task<PostingProfile> ResolveInvoiceProfileAsync(CancellationToken ct)
    {
        var active = await profiles.ListAsync(new Specification<PostingProfile>().Where(x =>
            x.Module == SalesSourceReferences.Module && x.DocumentType == SalesSourceReferences.SalesInvoice && x.IsActive), ct);
        if (active.Count != 1)
            throw new ConflictException("sales_posting_profile_missing", "يجب وجود Posting Profile فعال واحد لفاتورة المبيعات لاستخدامه في المرتجعات.");
        return active[0];
    }

    private async Task<Guid> RequiredRoleAsync(IReadOnlyList<PostingProfileLine> lines, string role, CancellationToken ct)
    {
        var matches = lines.Where(x => string.Equals(x.AccountRole, role, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1)
            throw new ConflictException("sales_return_posting_role_invalid", $"يجب تعريف حساب واحد للدور '{role}' في Posting Profile المبيعات.");
        await EnsureAccountAsync(matches[0].AccountId, ct);
        return matches[0].AccountId;
    }

    private async Task EnsureAccountAsync(Guid accountId, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(accountId, ct)
            ?? throw new ConflictException("sales_return_account_missing", "الحساب المحاسبي المطلوب غير موجود.");
        if (!account.CanReceivePosting())
            throw new ConflictException("sales_return_account_invalid", $"الحساب '{account.Code}' غير صالح للترحيل.");
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
