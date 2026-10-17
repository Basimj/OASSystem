using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Posting;

public sealed class SupplierSettlementFxPostingService(
    IRepository<JournalEntry, Guid> journals,
    IReadRepository<FiscalPeriod, Guid> fiscalPeriods,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<Currency, Guid> currencies,
    ISequenceNumberGenerator sequences)
    : ISupplierSettlementFxPostingService
{
    private const string SourceModule = "Accounting";
    private const string SourceDocumentType = "SupplierSettlementFx";

    public async Task<SupplierSettlementFxPostingResult> PostAsync(
        SupplierSettlementFxPostingRequest request,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.PaymentAllocationId == Guid.Empty)
            throw new ArgumentException("Payment allocation id is required.", nameof(request));
        if (request.SupplierId == Guid.Empty)
            throw new ArgumentException("Supplier id is required.", nameof(request));
        if (request.SupplierAccountId == Guid.Empty)
            throw new ArgumentException("Supplier account id is required.", nameof(request));
        if (request.SourceBaseAmount <= 0m || request.TargetBaseAmount <= 0m)
            throw new ConflictException("supplier_settlement_fx_amount_invalid", "قيم التسوية الأساسية يجب أن تكون أكبر من صفر.");

        var difference = Round(request.SourceBaseAmount - request.TargetBaseAmount);
        if (difference == 0m)
            return new SupplierSettlementFxPostingResult(null, 0m);

        var existing = await journals.ListAsync(
            new Specification<JournalEntry>()
                .Where(x =>
                    x.SourceModule == SourceModule &&
                    x.SourceDocumentType == SourceDocumentType &&
                    x.SourceDocumentId == request.PaymentAllocationId)
                .ApplyPaging(0, 2),
            cancellationToken);
        if (existing.Count > 0)
            return new SupplierSettlementFxPostingResult(existing[0].Id, difference);

        var accountingSettings = await settings.GetByIdAsync(AccountingSettings.SingletonId, cancellationToken)
            ?? throw new ConflictException("accounting_settings_required", "يجب إعداد المحاسبة قبل ترحيل فرق الصرف المحقق.");
        var baseCurrency = await currencies.GetByIdAsync(accountingSettings.BaseCurrencyId, cancellationToken)
            ?? throw new ConflictException("base_currency_missing", "العملة الأساسية المحددة في إعدادات المحاسبة غير موجودة.");

        await EnsurePostingAccountAsync(request.SupplierAccountId, cancellationToken);

        var fxAccountId = difference > 0m
            ? accountingSettings.ExchangeLossAccountId
            : accountingSettings.ExchangeGainAccountId;
        if (!fxAccountId.HasValue)
        {
            var code = difference > 0m ? "exchange_loss_account_required" : "exchange_gain_account_required";
            var message = difference > 0m
                ? "يجب تحديد حساب خسائر فروق العملة في إعدادات المحاسبة قبل إتمام السداد."
                : "يجب تحديد حساب أرباح فروق العملة في إعدادات المحاسبة قبل إتمام السداد.";
            throw new ConflictException(code, message);
        }
        await EnsurePostingAccountAsync(fxAccountId.Value, cancellationToken);

        var period = await ResolveFiscalPeriodAsync(request.PostingDate, cancellationToken);
        var sequence = await sequences.NextAsync($"JournalEntry-{request.PostingDate.Year}", cancellationToken);
        var journal = JournalEntry.Create(
            Guid.NewGuid(),
            $"JV-{request.PostingDate.Year:0000}-{sequence:000000}",
            JournalType.Automatic,
            request.PostingDate,
            request.PostingDate,
            period.Id,
            string.IsNullOrWhiteSpace(request.Description) ? "Supplier realized FX settlement" : request.Description.Trim(),
            SourceModule,
            SourceDocumentType,
            request.PaymentAllocationId,
            JournalEntryStatus.Draft);
        journal.SetBaseCurrencySnapshot(baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces);

        var amount = Math.Abs(difference);
        if (difference > 0m)
        {
            // Cash paid at the settlement rate is greater than the historical AP carrying
            // amount: Dr FX Loss / Cr Supplier AP to bring the supplier balance to zero.
            journal.AddLine(CreateBaseLine(
                journal.Id, 1, fxAccountId.Value, amount, 0m, baseCurrency,
                request.PostingDate, request.PaymentAllocationId, null,
                "Realized FX loss on supplier settlement"));
            journal.AddLine(CreateBaseLine(
                journal.Id, 2, request.SupplierAccountId, 0m, amount, baseCurrency,
                request.PostingDate, request.PaymentAllocationId, request.SupplierId,
                "Supplier AP realized FX adjustment"));
        }
        else
        {
            // Cash paid at the settlement rate is lower than the historical AP carrying
            // amount: Dr Supplier AP / Cr FX Gain to bring the supplier balance to zero.
            journal.AddLine(CreateBaseLine(
                journal.Id, 1, request.SupplierAccountId, amount, 0m, baseCurrency,
                request.PostingDate, request.PaymentAllocationId, request.SupplierId,
                "Supplier AP realized FX adjustment"));
            journal.AddLine(CreateBaseLine(
                journal.Id, 2, fxAccountId.Value, 0m, amount, baseCurrency,
                request.PostingDate, request.PaymentAllocationId, null,
                "Realized FX gain on supplier settlement"));
        }

        if (!journal.IsBalanced())
            throw new ConflictException("supplier_settlement_fx_unbalanced", "قيد فرق الصرف المحقق غير متوازن.");

        journal.SetPendingApproval();
        journal.Approve(postedBy, postedAtUtc);
        journal.Post(postedBy, postedAtUtc);
        await journals.AddAsync(journal, cancellationToken);

        return new SupplierSettlementFxPostingResult(journal.Id, difference);
    }

    private async Task<FiscalPeriod> ResolveFiscalPeriodAsync(DateOnly postingDate, CancellationToken ct)
    {
        var matches = await fiscalPeriods.ListAsync(
            new Specification<FiscalPeriod>()
                .Where(x => x.StartDate <= postingDate && x.EndDate >= postingDate)
                .ApplyPaging(0, 2),
            ct);
        if (matches.Count == 0)
            throw new ConflictException("fiscal_period_not_found", "لا توجد فترة مالية تغطي تاريخ ترحيل فرق الصرف.");
        if (matches.Count > 1)
            throw new ConflictException("fiscal_period_overlap", "يوجد أكثر من فترة مالية تغطي تاريخ ترحيل فرق الصرف.");
        if (!matches[0].CanPostAccounting())
            throw new ConflictException("fiscal_period_closed", "لا يمكن ترحيل فرق الصرف في فترة مالية مغلقة.");
        return matches[0];
    }

    private async Task EnsurePostingAccountAsync(Guid accountId, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(accountId, ct)
            ?? throw new NotFoundException(nameof(Account), accountId);
        if (!account.IsActive)
            throw new ConflictException("account_inactive", $"الحساب '{account.Code}' غير فعال.");
        if (account.AccountType == AccountType.Header || !account.IsPostingAccount)
            throw new ConflictException("account_not_postable", $"الحساب '{account.Code}' غير قابل للترحيل.");
    }

    private static JournalEntryLine CreateBaseLine(
        Guid journalId,
        int lineNumber,
        Guid accountId,
        decimal debit,
        decimal credit,
        Currency baseCurrency,
        DateOnly postingDate,
        Guid paymentAllocationId,
        Guid? supplierId,
        string description) =>
        JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(),
            journalId,
            lineNumber,
            accountId,
            debit,
            credit,
            baseCurrency.Id,
            baseCurrency.Code,
            baseCurrency.DecimalPlaces,
            debit,
            credit,
            1m,
            postingDate,
            ExchangeRateType.Accounting,
            ExchangeRateSource.System,
            description,
            null,
            supplierId,
            null,
            null,
            null,
            null,
            null,
            paymentAllocationId);

    private static decimal Round(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
