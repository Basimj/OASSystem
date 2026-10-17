using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Posting;

public sealed class CustomerSettlementFxPostingService(
    IRepository<JournalEntry, Guid> journals,
    IReadRepository<JournalEntryLine, Guid> journalLines,
    IReadRepository<FiscalPeriod, Guid> fiscalPeriods,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<AccountingSettings, Guid> settings,
    IReadRepository<Currency, Guid> currencies,
    ISequenceNumberGenerator sequences)
    : ICustomerSettlementFxPostingService
{
    private const string SourceModule = "Accounting";
    private const string SourceDocumentType = "CustomerSettlementFx";
    private const string ReversalSourceDocumentType = "CustomerSettlementFxReversal";

    public async Task<CustomerSettlementFxPostingResult> PostAsync(
        CustomerSettlementFxPostingRequest request,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PaymentAllocationId == Guid.Empty)
            throw new ArgumentException("Payment allocation id is required.", nameof(request));
        if (request.CustomerId == Guid.Empty)
            throw new ArgumentException("Customer id is required.", nameof(request));
        if (request.CustomerAccountId == Guid.Empty)
            throw new ArgumentException("Customer account id is required.", nameof(request));
        if (request.SourceBaseAmount <= 0m || request.TargetBaseAmount <= 0m)
            throw new ConflictException(
                "customer_settlement_fx_amount_invalid",
                "قيم التسوية الأساسية يجب أن تكون أكبر من صفر.");

        var difference = Round(request.SourceBaseAmount - request.TargetBaseAmount);
        if (difference == 0m)
            return new CustomerSettlementFxPostingResult(null, 0m);

        var existing = await journals.ListAsync(
            new Specification<JournalEntry>()
                .Where(x =>
                    x.SourceModule == SourceModule &&
                    x.SourceDocumentType == SourceDocumentType &&
                    x.SourceDocumentId == request.PaymentAllocationId)
                .ApplyPaging(0, 2),
            cancellationToken);

        if (existing.Count > 0)
            return new CustomerSettlementFxPostingResult(existing[0].Id, difference);

        var accountingSettings = await settings.GetByIdAsync(
            AccountingSettings.SingletonId,
            cancellationToken)
            ?? throw new ConflictException(
                "accounting_settings_required",
                "يجب إعداد المحاسبة قبل ترحيل فرق الصرف المحقق.");

        var baseCurrency = await currencies.GetByIdAsync(
            accountingSettings.BaseCurrencyId,
            cancellationToken)
            ?? throw new ConflictException(
                "base_currency_missing",
                "العملة الأساسية المحددة في إعدادات المحاسبة غير موجودة.");

        await EnsurePostingAccountAsync(request.CustomerAccountId, cancellationToken);

        var fxAccountId = difference > 0m
            ? accountingSettings.ExchangeGainAccountId
            : accountingSettings.ExchangeLossAccountId;

        if (!fxAccountId.HasValue)
        {
            throw new ConflictException(
                difference > 0m
                    ? "exchange_gain_account_required"
                    : "exchange_loss_account_required",
                difference > 0m
                    ? "يجب تحديد حساب أرباح فروق العملة في إعدادات المحاسبة قبل إتمام التحصيل."
                    : "يجب تحديد حساب خسائر فروق العملة في إعدادات المحاسبة قبل إتمام التحصيل.");
        }

        await EnsurePostingAccountAsync(fxAccountId.Value, cancellationToken);

        var period = await ResolveFiscalPeriodAsync(request.PostingDate, cancellationToken);
        var sequence = await sequences.NextAsync(
            $"JournalEntry-{request.PostingDate.Year}",
            cancellationToken);

        var journal = JournalEntry.Create(
            Guid.NewGuid(),
            $"JV-{request.PostingDate.Year:0000}-{sequence:000000}",
            JournalType.Automatic,
            request.PostingDate,
            request.PostingDate,
            period.Id,
            string.IsNullOrWhiteSpace(request.Description)
                ? "Customer realized FX settlement"
                : request.Description.Trim(),
            SourceModule,
            SourceDocumentType,
            request.PaymentAllocationId,
            JournalEntryStatus.Draft);

        journal.SetBaseCurrencySnapshot(
            baseCurrency.Id,
            baseCurrency.Code,
            baseCurrency.DecimalPlaces);

        var amount = Math.Abs(difference);

        if (difference > 0m)
        {
            // Receipt at a higher rate over-credits AR.
            // Restore AR then recognise the realised gain.
            journal.AddLine(CreateBaseLine(
                journal.Id, 1, request.CustomerAccountId,
                amount, 0m, baseCurrency, request.PostingDate,
                request.PaymentAllocationId, request.CustomerId,
                "Customer AR realized FX adjustment"));

            journal.AddLine(CreateBaseLine(
                journal.Id, 2, fxAccountId.Value,
                0m, amount, baseCurrency, request.PostingDate,
                request.PaymentAllocationId, null,
                "Realized FX gain on customer settlement"));
        }
        else
        {
            // Receipt at a lower rate under-credits AR.
            // Expense the loss and clear the remaining AR.
            journal.AddLine(CreateBaseLine(
                journal.Id, 1, fxAccountId.Value,
                amount, 0m, baseCurrency, request.PostingDate,
                request.PaymentAllocationId, null,
                "Realized FX loss on customer settlement"));

            journal.AddLine(CreateBaseLine(
                journal.Id, 2, request.CustomerAccountId,
                0m, amount, baseCurrency, request.PostingDate,
                request.PaymentAllocationId, request.CustomerId,
                "Customer AR realized FX adjustment"));
        }

        if (!journal.IsBalanced())
            throw new ConflictException(
                "customer_settlement_fx_unbalanced",
                "قيد فرق الصرف المحقق للعميل غير متوازن.");

        journal.SetPendingApproval();
        journal.Approve(postedBy, postedAtUtc);
        journal.Post(postedBy, postedAtUtc);

        await journals.AddAsync(journal, cancellationToken);

        return new CustomerSettlementFxPostingResult(journal.Id, difference);
    }

    public async Task<CustomerSettlementFxReversalResult> ReverseAsync(
        CustomerSettlementFxReversalRequest request,
        Guid reversedBy,
        DateTime reversedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PaymentAllocationId == Guid.Empty)
            throw new ArgumentException("Payment allocation id is required.", nameof(request));
        if (reversedBy == Guid.Empty)
            throw new ArgumentException("Reversed by is required.", nameof(reversedBy));

        var originals = await journals.ListAsync(
            new Specification<JournalEntry>()
                .Where(x =>
                    x.SourceModule == SourceModule &&
                    x.SourceDocumentType == SourceDocumentType &&
                    x.SourceDocumentId == request.PaymentAllocationId)
                .ApplyPaging(0, 2),
            cancellationToken);

        if (originals.Count == 0)
            return new CustomerSettlementFxReversalResult(null, null);

        var original = await journals.GetForUpdateAsync(
            originals[0].Id,
            cancellationToken)
            ?? throw new NotFoundException(nameof(JournalEntry), originals[0].Id);

        if (original.Status != JournalEntryStatus.Posted)
            throw new ConflictException(
                "customer_settlement_fx_not_posted",
                "قيد فرق الصرف الأصلي غير مرحل ولا يمكن عكسه.");

        if (original.ReversedJournalId.HasValue)
            return new CustomerSettlementFxReversalResult(
                original.Id,
                original.ReversedJournalId.Value);

        var existingReversal = await journals.ListAsync(
            new Specification<JournalEntry>()
                .Where(x =>
                    x.SourceModule == SourceModule &&
                    x.SourceDocumentType == ReversalSourceDocumentType &&
                    x.SourceDocumentId == request.PaymentAllocationId)
                .ApplyPaging(0, 2),
            cancellationToken);

        if (existingReversal.Count > 0)
        {
            original.MarkReversed(existingReversal[0].Id);
            journals.Update(original);
            return new CustomerSettlementFxReversalResult(
                original.Id,
                existingReversal[0].Id);
        }

        if (!original.BaseCurrencyId.HasValue ||
            string.IsNullOrWhiteSpace(original.BaseCurrencyCodeSnapshot) ||
            !original.BaseCurrencyDecimalPlacesSnapshot.HasValue)
        {
            throw new ConflictException(
                "customer_settlement_fx_base_currency_missing",
                "قيد فرق الصرف الأصلي لا يحتوي Snapshot صالحًا للعملة الأساسية.");
        }

        var originalLines = await journalLines.ListAsync(
            new Specification<JournalEntryLine>()
                .Where(x => x.JournalEntryId == original.Id),
            cancellationToken);

        if (originalLines.Count == 0)
            throw new ConflictException(
                "customer_settlement_fx_lines_missing",
                "قيد فرق الصرف الأصلي لا يحتوي بنودًا قابلة للعكس.");

        var period = await ResolveFiscalPeriodAsync(
            request.PostingDate,
            cancellationToken);
        var sequence = await sequences.NextAsync(
            $"JournalEntry-{request.PostingDate.Year}",
            cancellationToken);

        var reversal = JournalEntry.Create(
            Guid.NewGuid(),
            $"JV-{request.PostingDate.Year:0000}-{sequence:000000}",
            JournalType.Automatic,
            request.PostingDate,
            request.PostingDate,
            period.Id,
            string.IsNullOrWhiteSpace(request.Description)
                ? "Reverse customer realized FX settlement"
                : request.Description.Trim(),
            SourceModule,
            ReversalSourceDocumentType,
            request.PaymentAllocationId,
            JournalEntryStatus.Draft);

        reversal.SetBaseCurrencySnapshot(
            original.BaseCurrencyId.Value,
            original.BaseCurrencyCodeSnapshot,
            original.BaseCurrencyDecimalPlacesSnapshot.Value);

        var lineNumber = 1;
        foreach (var source in originalLines.OrderBy(x => x.LineNumber))
        {
            var currencyId =
                source.TransactionCurrencyId ?? original.BaseCurrencyId.Value;
            var currencyCode =
                source.TransactionCurrencyCodeSnapshot ??
                original.BaseCurrencyCodeSnapshot;
            var currencyDecimals =
                source.TransactionCurrencyDecimalPlacesSnapshot ??
                original.BaseCurrencyDecimalPlacesSnapshot.Value;
            var transactionDebit =
                source.TransactionCreditAmount ?? source.CreditAmount;
            var transactionCredit =
                source.TransactionDebitAmount ?? source.DebitAmount;

            reversal.AddLine(JournalEntryLine.CreateMultiCurrency(
                Guid.NewGuid(),
                reversal.Id,
                lineNumber++,
                source.AccountId,
                source.CreditAmount,
                source.DebitAmount,
                currencyId,
                currencyCode,
                currencyDecimals,
                transactionDebit,
                transactionCredit,
                source.ExchangeRate ?? 1m,
                source.ExchangeRateDate ?? original.PostingDate,
                source.ExchangeRateType ?? ExchangeRateType.Accounting,
                source.ExchangeRateSource ?? ExchangeRateSource.System,
                $"Reversal: {source.Description ?? original.Description}",
                source.CustomerId,
                source.SupplierId,
                source.EmployeeId,
                source.PartyNameSnapshot,
                source.CostCenterId,
                source.ProductVariantId,
                source.WarehouseId,
                request.PaymentAllocationId));
        }

        if (!reversal.IsBalanced())
            throw new ConflictException(
                "customer_settlement_fx_reversal_unbalanced",
                "قيد عكس فرق الصرف للعميل غير متوازن.");

        reversal.SetPendingApproval();
        reversal.Approve(reversedBy, reversedAtUtc);
        reversal.Post(reversedBy, reversedAtUtc);

        await journals.AddAsync(reversal, cancellationToken);

        original.MarkReversed(reversal.Id);
        journals.Update(original);

        return new CustomerSettlementFxReversalResult(
            original.Id,
            reversal.Id);
    }

    private async Task<FiscalPeriod> ResolveFiscalPeriodAsync(
        DateOnly postingDate,
        CancellationToken cancellationToken)
    {
        var matches = await fiscalPeriods.ListAsync(
            new Specification<FiscalPeriod>()
                .Where(x =>
                    x.StartDate <= postingDate &&
                    x.EndDate >= postingDate)
                .ApplyPaging(0, 2),
            cancellationToken);

        if (matches.Count == 0)
            throw new ConflictException(
                "fiscal_period_not_found",
                "لا توجد فترة مالية تغطي تاريخ ترحيل فرق الصرف.");
        if (matches.Count > 1)
            throw new ConflictException(
                "fiscal_period_overlap",
                "يوجد أكثر من فترة مالية تغطي تاريخ ترحيل فرق الصرف.");
        if (!matches[0].CanPostAccounting())
            throw new ConflictException(
                "fiscal_period_closed",
                "لا يمكن ترحيل فرق الصرف في فترة مالية مغلقة.");

        return matches[0];
    }

    private async Task EnsurePostingAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

        if (!account.IsActive)
            throw new ConflictException(
                "account_inactive",
                $"الحساب '{account.Code}' غير فعال.");

        if (account.AccountType == AccountType.Header ||
            !account.IsPostingAccount)
            throw new ConflictException(
                "account_not_postable",
                $"الحساب '{account.Code}' غير قابل للترحيل.");
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
        Guid? customerId,
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
            customerId,
            null,
            null,
            null,
            null,
            null,
            null,
            paymentAllocationId);

    private static decimal Round(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
