using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Posting;

public sealed class SalesInvoiceAccountingPostingService(
    IRepository<JournalEntry, Guid> journals,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<PostingProfile, Guid> profiles,
    IReadRepository<PostingProfileLine, Guid> profileLines,
    ISalesPostingPeriodService postingPeriods,
    ISequenceNumberGenerator sequences) : ISalesInvoiceAccountingPostingService
{
    private const string SalesRevenueRole = "SalesRevenue";
    private const string TaxPayableRole = "TaxPayable";
    private const string InventoryRole = "Inventory";
    private const string CogsRole = "COGS";

    public async Task<Guid> PostAsync(
        SalesInvoice invoice,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var period = await postingPeriods.GetOpenPostingPeriodAsync(
            invoice.PostingDate,
            invoice.Lines.Any(x => x.IsActive && x.RequiresInventory),
            cancellationToken);
        var customer = await customers.GetByIdAsync(invoice.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), invoice.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال ولا يمكن ترحيل الفاتورة.");

        await EnsureAccountAsync(customer.AccountId, SalesErrorCodes.CustomerAccountInvalid, cancellationToken);

        var profileSpec = new Specification<PostingProfile>()
            .Where(x => x.Module == SalesSourceReferences.Module &&
                        x.DocumentType == SalesSourceReferences.SalesInvoice &&
                        x.IsActive);

        var activeProfiles = await profiles.ListAsync(profileSpec, cancellationToken);

        if (activeProfiles.Count == 0)
        {
            throw new ConflictException(
                "sales_posting_profile_missing",
                "لم يتم إعداد Posting Profile فعال لفاتورة المبيعات.");
        }

        if (activeProfiles.Count > 1)
        {
            throw new ConflictException(
                "sales_posting_profile_duplicate",
                "يوجد أكثر من Posting Profile فعال لفاتورة المبيعات. يجب إبقاء ملف ترحيل واحد فعال فقط للموديول Sales ونوع المستند SalesInvoice.");
        }

        var profile = activeProfiles[0];
        var lineSpec = new Specification<PostingProfileLine>()
            .Where(x => x.PostingProfileId == profile.Id);
        var configured = await profileLines.ListAsync(lineSpec, cancellationToken);

        var salesRevenueAccount = await RequiredRoleAsync(configured, SalesRevenueRole, cancellationToken);
        Guid? taxAccount = invoice.TaxAmount > 0m
            ? await RequiredRoleAsync(configured, TaxPayableRole, cancellationToken)
            : null;
        var inventoryLines = invoice.Lines.Where(x => x.IsActive && x.RequiresInventory).ToList();
        Guid? inventoryAccount = inventoryLines.Count > 0
            ? await RequiredRoleAsync(configured, InventoryRole, cancellationToken)
            : null;
        Guid? cogsAccount = inventoryLines.Count > 0
            ? await RequiredRoleAsync(configured, CogsRole, cancellationToken)
            : null;

        var sequence = await sequences.NextAsync($"JournalEntry-{invoice.PostingDate.Year}", cancellationToken);
        var journal = JournalEntry.Create(
            Guid.NewGuid(),
            $"JV-{invoice.PostingDate.Year:0000}-{sequence:000000}",
            JournalType.Automatic,
            invoice.PostingDate,
            invoice.InvoiceDate,
            period.Id,
            $"Sales invoice {invoice.InvoiceCode}",
            SalesSourceReferences.Module,
            SalesSourceReferences.SalesInvoice,
            invoice.Id,
            JournalEntryStatus.Draft);
        journal.SetBaseCurrencySnapshot(invoice.BaseCurrencyId, invoice.BaseCurrencyCodeSnapshot, invoice.BaseCurrencyDecimalPlacesSnapshot);

        var number = 1;
        journal.AddLine(CreateInvoiceCurrencyLine(
            journal.Id, number++, customer.AccountId, invoice.BaseTotalAmount, 0m,
            invoice.TotalAmount, 0m, invoice, $"Customer / {invoice.InvoiceCode}", customer.Id, null, null, null));

        // Use the confirmed invoice header snapshots so base-currency rounding remains
        // internally balanced even when line-level conversions have rounding differences.
        var transactionNet = invoice.TotalAmount - invoice.TaxAmount;
        var baseNet = invoice.BaseTotalAmount - invoice.BaseTaxAmount;
        journal.AddLine(CreateInvoiceCurrencyLine(
            journal.Id, number++, salesRevenueAccount, 0m, baseNet,
            0m, transactionNet, invoice, $"Sales revenue / {invoice.InvoiceCode}", null, null, null, null));

        if (invoice.TaxAmount > 0m && taxAccount.HasValue)
        {
            journal.AddLine(CreateInvoiceCurrencyLine(
                journal.Id, number++, taxAccount.Value, 0m, invoice.BaseTaxAmount,
                0m, invoice.TaxAmount, invoice, $"Sales tax / {invoice.InvoiceCode}", null, null, null, null));
        }

        foreach (var salesLine in inventoryLines.OrderBy(x => x.LineNumber))
        {
            if (!salesLine.TotalCostSnapshot.HasValue)
                throw new ConflictException("sales_cost_snapshot_missing", "تكلفة المخزون غير محفوظة لجميع أسطر الفاتورة.");
            var cost = salesLine.TotalCostSnapshot.Value;
            journal.AddLine(CreateBaseCurrencyLine(
                journal.Id, number++, cogsAccount!.Value, cost, 0m, invoice,
                $"COGS / {invoice.InvoiceCode} / line {salesLine.LineNumber}", salesLine.ProductVariantId, salesLine.WarehouseId, salesLine.Id));
            journal.AddLine(CreateBaseCurrencyLine(
                journal.Id, number++, inventoryAccount!.Value, 0m, cost, invoice,
                $"Inventory / {invoice.InvoiceCode} / line {salesLine.LineNumber}", salesLine.ProductVariantId, salesLine.WarehouseId, salesLine.Id));
        }

        journal.SetPendingApproval();
        journal.Approve(postedBy, postedAtUtc);
        journal.Post(postedBy, postedAtUtc);
        await journals.AddAsync(journal, cancellationToken);
        return journal.Id;
    }

    private async Task<Guid> RequiredRoleAsync(
        IReadOnlyList<PostingProfileLine> lines,
        string role,
        CancellationToken cancellationToken)
    {
        var matchingLines = lines
            .Where(x => string.Equals(x.AccountRole, role, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matchingLines.Count == 0)
        {
            throw new ConflictException(
                "sales_posting_role_missing",
                $"Posting Profile does not define required role '{role}'.");
        }

        if (matchingLines.Count > 1)
        {
            throw new ConflictException(
                "sales_posting_role_duplicate",
                $"Posting Profile يحتوي أكثر من حساب للدور '{role}'. يجب تعريف حساب واحد فقط لكل دور ترحيل.");
        }

        var line = matchingLines[0];
        await EnsureAccountAsync(line.AccountId, "sales_posting_account_invalid", cancellationToken);
        return line.AccountId;
    }

    private async Task EnsureAccountAsync(Guid accountId, string errorCode, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new ConflictException(errorCode, "الحساب المحاسبي المطلوب غير موجود.");
        if (!account.CanReceivePosting())
            throw new ConflictException(errorCode, $"الحساب '{account.Code}' غير صالح للترحيل النظامي.");
    }

    private static JournalEntryLine CreateInvoiceCurrencyLine(
        Guid journalId, int lineNumber, Guid accountId, decimal debitBase, decimal creditBase,
        decimal debitTransaction, decimal creditTransaction, SalesInvoice invoice, string description,
        Guid? customerId, Guid? productVariantId, Guid? warehouseId, Guid? sourceLineId) =>
        JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journalId, lineNumber, accountId,
            debitBase, creditBase,
            invoice.CurrencyId, invoice.CurrencyCodeSnapshot, invoice.CurrencyDecimalPlacesSnapshot,
            debitTransaction, creditTransaction,
            invoice.ExchangeRate, invoice.ExchangeRateDate, invoice.ExchangeRateType, invoice.ExchangeRateSource,
            description, customerId, null, null, null, null, productVariantId, warehouseId, sourceLineId);

    private static JournalEntryLine CreateBaseCurrencyLine(
        Guid journalId, int lineNumber, Guid accountId, decimal debit, decimal credit,
        SalesInvoice invoice, string description, Guid? productVariantId, Guid? warehouseId, Guid? sourceLineId) =>
        JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journalId, lineNumber, accountId,
            debit, credit,
            invoice.BaseCurrencyId, invoice.BaseCurrencyCodeSnapshot, invoice.BaseCurrencyDecimalPlacesSnapshot,
            debit, credit,
            1m, invoice.PostingDate, ExchangeRateType.Accounting, ExchangeRateSource.System,
            description, null, null, null, null, null, productVariantId, warehouseId, sourceLineId);
}
