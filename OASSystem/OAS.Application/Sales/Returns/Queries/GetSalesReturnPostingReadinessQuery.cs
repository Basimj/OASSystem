using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Returns.Services;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;
using DomainSalesInvoiceStatus = OAS.Domain.Sales.Enums.SalesInvoiceStatus;
using DomainSalesReturnStatus = OAS.Domain.Sales.Enums.SalesReturnStatus;

namespace OAS.Application.Sales.Returns.Queries;

public sealed record GetSalesReturnPostingReadinessQuery(Guid Id)
    : IQuery<SalesReturnPostingReadinessDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.Post];
}

/// <summary>
/// A read-only preflight. Expected business-invalid states are returned as data,
/// never raised as ConflictException in a debugger. Posting remains authoritative.
/// </summary>
public sealed class GetSalesReturnPostingReadinessQueryHandler(
    ISalesReturnAggregateRepository returns,
    IReadRepository<SalesInvoice, Guid> invoices,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<JournalEntry, Guid> journals,
    IReadRepository<JournalEntryLine, Guid> journalLines,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<FiscalPeriod, Guid> periods)
    : IRequestHandler<GetSalesReturnPostingReadinessQuery, SalesReturnPostingReadinessDto>
{
    public async Task<SalesReturnPostingReadinessDto> Handle(GetSalesReturnPostingReadinessQuery request, CancellationToken ct)
    {
        var issues = new List<SalesReturnPostingIssueDto>();
        void Add(string code, string message) => issues.Add(new SalesReturnPostingIssueDto(code, message));
        SalesReturnPostingReadinessDto Result() => new(request.Id, issues.Count == 0, issues);

        var salesReturn = await returns.GetAggregateAsync(request.Id, false, ct);
        if (salesReturn is null)
        {
            Add("sales_return_not_found", "المرتجع غير موجود.");
            return Result();
        }
        if (salesReturn.Status != DomainSalesReturnStatus.Confirmed)
            Add("sales_return_invalid_status", "يجب تأكيد المرتجع قبل الترحيل.");

        var customer = await customers.GetByIdAsync(salesReturn.CustomerId, ct);
        if (customer is null || !customer.IsActive)
            Add("sales_return_customer_inactive", "العميل غير فعال أو غير موجود.");

        var pastJournals = await journals.ListAsync(new Specification<JournalEntry>().Where(x =>
            x.SourceModule == SalesSourceReferences.Module &&
            x.SourceDocumentType == SalesSourceReferences.SalesReturn &&
            x.SourceDocumentId == salesReturn.Id), ct);
        if (pastJournals.Count > 0)
            Add("sales_return_journal_already_exists", "يوجد قيد سابق لهذا المرتجع ويجب مراجعته قبل إعادة الترحيل.");

        var requiresInventory = salesReturn.Lines.Any(x => x.IsActive && x.RestoresInventoryAsset);
        var matchingPeriods = await periods.ListAsync(new Specification<FiscalPeriod>().Where(x =>
            x.StartDate <= salesReturn.PostingDate && x.EndDate >= salesReturn.PostingDate), ct);
        var period = matchingPeriods.OrderBy(x => x.StartDate).FirstOrDefault();
        if (period is null)
            Add("sales_return_period_missing", "لا توجد فترة مالية تغطي تاريخ ترحيل المرتجع.");
        else if (period.Status == FiscalPeriodStatus.Closed || period.SalesLocked ||
                 period.AccountingLocked || (requiresInventory && period.InventoryLocked))
            Add("sales_return_period_locked", "الفترة المالية مغلقة أو مقفلة للمبيعات أو المحاسبة أو المخزون.");

        var invoice = await invoices.GetByIdAsync(salesReturn.SalesInvoiceId, ct);
        if (invoice is null || invoice.Status != DomainSalesInvoiceStatus.Posted || !invoice.JournalEntryId.HasValue)
        {
            Add("sales_return_source_journal_missing", "قيد الفاتورة الأصلية غير متاح.");
            return Result();
        }
        if (salesReturn.PostingDate < invoice.PostingDate || salesReturn.ReturnDate > salesReturn.PostingDate)
            Add("sales_return_dates_invalid", "تواريخ المرتجع والترحيل غير متوافقة مع الفاتورة الأصلية.");

        var lines = await journalLines.ListAsync(new Specification<JournalEntryLine>()
            .Where(x => x.JournalEntryId == invoice.JournalEntryId.Value), ct);
        if (lines.Count == 0)
        {
            Add("sales_return_source_journal_lines_missing", "قيد الفاتورة الأصلية بلا أسطر قابلة للفحص.");
            return Result();
        }

        async Task CheckAccount(Guid id)
        {
            var account = await accounts.GetByIdAsync(id, ct);
            if (account is null || !account.IsPostingAccount || account.AccountType == AccountType.Header)
                Add("sales_return_historical_account_invalid", "أحد الحسابات التاريخية غير صالح للترحيل.");
        }

        if (!SalesReturnHistoricalAccountResolver.TryResolveCustomerAccount(lines, salesReturn.CustomerId, out var customerAccount))
        {
            Add("sales_return_source_customer_account_missing", "تعذر تحديد حساب العميل من القيد التاريخي.");
            return Result();
        }
        await CheckAccount(customerAccount);

        var invoiceNet = invoice.BaseTotalAmount - invoice.BaseTaxAmount;
        if (!SalesReturnHistoricalAccountResolver.TryResolveRevenueAccount(
                lines, customerAccount, invoiceNet, invoice.BaseTaxAmount, out var revenueAccount))
        {
            Add("sales_return_legacy_revenue_role_review_required", "حساب الإيراد التاريخي غير محدد بصورة فريدة؛ راجع قيد الفاتورة.");
            return Result();
        }
        await CheckAccount(revenueAccount);

        if (salesReturn.TaxAmount > 0m)
        {
            if (!SalesReturnHistoricalAccountResolver.TryResolveTaxAccount(
                    lines, customerAccount, revenueAccount, invoice.BaseTaxAmount, out var taxAccount))
                Add("sales_return_legacy_tax_role_review_required", "حساب ضريبة الفاتورة التاريخي غير معروف بشكل فريد.");
            else
                await CheckAccount(taxAccount);
        }

        foreach (var line in salesReturn.Lines.Where(x => x.IsActive && x.RestoresInventoryAsset && (x.TotalCostSnapshot ?? 0m) > 0m))
        {
            if (!SalesReturnHistoricalAccountResolver.TryResolveSourceLineAccount(
                    lines, line.SalesInvoiceLineId, AccountingPostingRoles.SalesInventory, true, out var inventoryAccount))
                Add("sales_return_source_inventory_account_missing", $"لا يمكن إثبات حساب المخزون التاريخي للسطر {line.LineNumber}.");
            else
                await CheckAccount(inventoryAccount);

            if (!SalesReturnHistoricalAccountResolver.TryResolveSourceLineAccount(
                    lines, line.SalesInvoiceLineId, AccountingPostingRoles.SalesCOGS, false, out var cogsAccount))
                Add("sales_return_source_cogs_account_missing", $"لا يمكن إثبات حساب تكلفة المبيعات التاريخي للسطر {line.LineNumber}.");
            else
                await CheckAccount(cogsAccount);
        }
        return Result();
    }
}
