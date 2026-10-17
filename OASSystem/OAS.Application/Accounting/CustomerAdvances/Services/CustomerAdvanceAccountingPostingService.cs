using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Accounting.CustomerAdvances.Services;

public sealed class CustomerAdvanceAccountingPostingService(
    IRepository<JournalEntry, Guid> journals,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<FiscalPeriod, Guid> fiscalPeriods,
    IReadRepository<PostingProfile, Guid> profiles,
    IReadRepository<PostingProfileLine, Guid> profileLines,
    ISequenceNumberGenerator sequences,
    IReadRepository<AccountingSettings, Guid>? accountingSettings = null)
    : ICustomerAdvanceAccountingPostingService
{
    private const string Module = "Accounting";
    private const string DocumentType = "CustomerAdvanceApplication";
    private const string CustomerAdvancesRole = "CustomerAdvances";

    public async Task<CustomerAdvanceApplicationPostingResult> PostApplicationAsync(
        Guid applicationId,
        CustomerAdvance advance,
        SalesInvoice invoice,
        decimal amount,
        decimal sourceBaseAmount,
        decimal targetBaseAmount,
        string? appliedBy,
        DateTime appliedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(advance);
        ArgumentNullException.ThrowIfNull(invoice);
        if (applicationId == Guid.Empty)
            throw new ArgumentException("Customer advance application id is required.", nameof(applicationId));
        if (amount <= 0m || sourceBaseAmount <= 0m || targetBaseAmount <= 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Advance application amounts must be greater than zero.");
        if (!Guid.TryParse(appliedBy, out var actorId) || actorId == Guid.Empty)
            throw new ForbiddenException();

        var customer = await customers.GetByIdAsync(advance.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), advance.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException("customer_advance_customer_inactive", "العميل غير فعال ولا يمكن تطبيق العربون.");
        await EnsurePostingAccountAsync(customer.AccountId, cancellationToken);

        var advanceAccountId = await ResolveAdvanceLiabilityAccountAsync(cancellationToken);
        var postingDate = DateOnly.FromDateTime(appliedAtUtc);
        var period = await ResolveAccountingPeriodAsync(postingDate, cancellationToken);

        Guid? gainAccountId = null;
        Guid? lossAccountId = null;
        var difference = Math.Round(sourceBaseAmount - targetBaseAmount, 4, MidpointRounding.AwayFromZero);
        if (difference != 0m)
        {
            if (accountingSettings is null)
                throw new ConflictException(
                    "customer_advance_fx_settings_missing",
                    "إعدادات فروقات العملة غير متاحة لتطبيق العربون متعدد العملات.");

            var settings = await accountingSettings.GetByIdAsync(AccountingSettings.SingletonId, cancellationToken)
                ?? throw new ConflictException("accounting_settings_required", "يجب إعداد المحاسبة قبل تطبيق العربون متعدد العملات.");

            if (difference > 0m)
            {
                gainAccountId = settings.ExchangeGainAccountId
                    ?? throw new ConflictException("exchange_gain_account_required", "يجب تحديد حساب أرباح فروق العملة في إعدادات المحاسبة.");
                await EnsurePostingAccountAsync(gainAccountId.Value, cancellationToken);
            }
            else
            {
                lossAccountId = settings.ExchangeLossAccountId
                    ?? throw new ConflictException("exchange_loss_account_required", "يجب تحديد حساب خسائر فروق العملة في إعدادات المحاسبة.");
                await EnsurePostingAccountAsync(lossAccountId.Value, cancellationToken);
            }
        }

        var sequence = await sequences.NextAsync($"JournalEntry-{postingDate.Year}", cancellationToken);
        var journal = JournalEntry.Create(
            Guid.NewGuid(),
            $"JV-{postingDate.Year:0000}-{sequence:000000}",
            JournalType.Automatic,
            postingDate,
            postingDate,
            period.Id,
            $"Apply customer advance {advance.AdvanceNumber}",
            Module,
            DocumentType,
            applicationId,
            JournalEntryStatus.Draft);
        journal.SetBaseCurrencySnapshot(
            advance.BaseCurrencyId,
            advance.BaseCurrencyCodeSnapshot,
            advance.BaseCurrencyDecimalPlacesSnapshot);

        // Dr customer-advance liability at the source carrying amount.
        journal.AddLine(JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journal.Id, 1, advanceAccountId,
            sourceBaseAmount, 0m,
            advance.CurrencyId, advance.CurrencyCodeSnapshot, advance.CurrencyDecimalPlacesSnapshot,
            amount, 0m,
            advance.ExchangeRate, advance.ExchangeRateDate, advance.ExchangeRateType, advance.ExchangeRateSource,
            $"Customer advance / {advance.AdvanceNumber}",
            advance.CustomerId, null, null, null, null, null, null, applicationId));

        // Cr AR at the historical carrying amount being extinguished.
        journal.AddLine(JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journal.Id, 2, customer.AccountId,
            0m, targetBaseAmount,
            invoice.CurrencyId, invoice.CurrencyCodeSnapshot, invoice.CurrencyDecimalPlacesSnapshot,
            0m, Math.Round(targetBaseAmount / invoice.ExchangeRate, invoice.CurrencyDecimalPlacesSnapshot, MidpointRounding.AwayFromZero),
            invoice.ExchangeRate, invoice.ExchangeRateDate, invoice.ExchangeRateType, invoice.ExchangeRateSource,
            $"Customer receivable / {invoice.InvoiceCode}",
            advance.CustomerId, null, null, null, null, null, null, applicationId));

        if (difference > 0m)
        {
            // Source liability carrying value exceeds historical AR extinguished.
            journal.AddLine(JournalEntryLine.CreateMultiCurrency(
                Guid.NewGuid(), journal.Id, 3, gainAccountId!.Value,
                0m, difference,
                advance.BaseCurrencyId, advance.BaseCurrencyCodeSnapshot, advance.BaseCurrencyDecimalPlacesSnapshot,
                0m, difference,
                1m, postingDate, ExchangeRateType.Accounting, ExchangeRateSource.System,
                "Realized FX gain on customer advance application",
                advance.CustomerId, null, null, null, null, null, null, applicationId));
        }
        else if (difference < 0m)
        {
            var loss = Math.Abs(difference);
            journal.AddLine(JournalEntryLine.CreateMultiCurrency(
                Guid.NewGuid(), journal.Id, 3, lossAccountId!.Value,
                loss, 0m,
                advance.BaseCurrencyId, advance.BaseCurrencyCodeSnapshot, advance.BaseCurrencyDecimalPlacesSnapshot,
                loss, 0m,
                1m, postingDate, ExchangeRateType.Accounting, ExchangeRateSource.System,
                "Realized FX loss on customer advance application",
                advance.CustomerId, null, null, null, null, null, null, applicationId));
        }

        if (!journal.IsBalanced())
            throw new ConflictException("customer_advance_application_unbalanced", "قيد تطبيق عربون العميل غير متوازن.");

        journal.SetPendingApproval();
        journal.Approve(actorId, appliedAtUtc);
        journal.Post(actorId, appliedAtUtc);
        await journals.AddAsync(journal, cancellationToken);

        return new CustomerAdvanceApplicationPostingResult(journal.Id, targetBaseAmount);
    }

    private async Task<Guid> ResolveAdvanceLiabilityAccountAsync(CancellationToken cancellationToken)
    {
        var profileSpec = new Specification<PostingProfile>()
            .Where(x => x.Module == Module && x.DocumentType == DocumentType && x.IsActive);
        var activeProfiles = await profiles.ListAsync(profileSpec, cancellationToken);

        if (activeProfiles.Count == 0)
            throw new ConflictException("customer_advance_posting_profile_missing", "لم يتم إعداد حساب دفعات مقدمة من العملاء. افتح إعدادات المحاسبة وحدد «حساب دفعات مقدمة من العملاء» ثم احفظ الإعدادات.");
        if (activeProfiles.Count > 1)
            throw new ConflictException("customer_advance_posting_profile_duplicate", "يوجد أكثر من إعداد ترحيل فعال لعربون العميل. راجع Posting Profiles وأبقِ إعدادًا واحدًا فعالًا فقط.");

        var profileId = activeProfiles[0].Id;
        var lineSpec = new Specification<PostingProfileLine>()
            .Where(x => x.PostingProfileId == profileId && x.AccountRole == CustomerAdvancesRole);
        var roleLines = await profileLines.ListAsync(lineSpec, cancellationToken);
        if (roleLines.Count == 0)
            throw new ConflictException("customer_advance_posting_role_missing", "إعداد عربون العميل لا يحتوي حساب دفعات مقدمة من العملاء. احفظ الحساب من إعدادات المحاسبة.");
        if (roleLines.Count > 1)
            throw new ConflictException("customer_advance_posting_role_duplicate", "يوجد أكثر من حساب معرف لدفعات مقدمة من العملاء. يجب إبقاء حساب واحد فقط.");

        await EnsurePostingAccountAsync(roleLines[0].AccountId, cancellationToken);
        return roleLines[0].AccountId;
    }

    private async Task<FiscalPeriod> ResolveAccountingPeriodAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var spec = new Specification<FiscalPeriod>()
            .Where(x => x.StartDate <= date && x.EndDate >= date);
        var candidates = await fiscalPeriods.ListAsync(spec, cancellationToken);
        var usable = candidates.Where(x => x.CanPostAccounting()).ToList();

        if (usable.Count == 0)
            throw new ConflictException("customer_advance_fiscal_period_closed", "لا توجد فترة مالية مفتوحة تسمح بالترحيل في تاريخ تطبيق العربون.");
        if (usable.Count > 1)
            throw new ConflictException("customer_advance_fiscal_period_ambiguous", "يوجد أكثر من فترة مالية صالحة لتاريخ تطبيق العربون.");
        return usable[0];
    }

    private async Task EnsurePostingAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new ConflictException("customer_advance_account_missing", "الحساب المحاسبي المطلوب غير موجود.");
        if (!account.CanReceivePosting())
            throw new ConflictException("customer_advance_account_invalid", $"الحساب '{account.Code}' غير صالح للترحيل النظامي.");
    }
}
