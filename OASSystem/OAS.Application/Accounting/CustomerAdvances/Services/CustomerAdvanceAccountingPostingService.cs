using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Accounting.CustomerAdvances.Services;

/// <summary>
/// Posts the accounting settlement produced when a customer advance is applied to a posted sales invoice.
/// The advance liability account is resolved from Posting Profiles; no GL account is hard-coded here.
/// </summary>
public sealed class CustomerAdvanceAccountingPostingService(
    IRepository<JournalEntry, Guid> journals,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Account, Guid> accounts,
    IReadRepository<FiscalPeriod, Guid> fiscalPeriods,
    IReadRepository<PostingProfile, Guid> profiles,
    IReadRepository<PostingProfileLine, Guid> profileLines,
    ISequenceNumberGenerator sequences) : ICustomerAdvanceAccountingPostingService
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

        // The current allocation validator evaluates a cross-currency target using the
        // authoritative base amount. Until a dedicated realised FX settlement role is
        // introduced, the application journal must remain exactly balanced in base currency.
        if (sourceBaseAmount != targetBaseAmount)
        {
            throw new ConflictException(
                "customer_advance_fx_difference_not_configured",
                "لا يمكن تطبيق العربون لأن القيمة الأساسية للمصدر والمستند المستهدف مختلفة ولا يوجد حساب فروقات عملة مخصص لهذه التسوية.");
        }

        var customer = await customers.GetByIdAsync(advance.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), advance.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException("customer_advance_customer_inactive", "العميل غير فعال ولا يمكن تطبيق العربون.");
        await EnsurePostingAccountAsync(customer.AccountId, cancellationToken);

        var advanceAccountId = await ResolveAdvanceLiabilityAccountAsync(cancellationToken);
        var postingDate = DateOnly.FromDateTime(appliedAtUtc);
        var period = await ResolveAccountingPeriodAsync(postingDate, cancellationToken);

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

        // Dr Customer Advances Liability
        journal.AddLine(JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journal.Id, 1, advanceAccountId,
            sourceBaseAmount, 0m,
            advance.CurrencyId, advance.CurrencyCodeSnapshot, advance.CurrencyDecimalPlacesSnapshot,
            amount, 0m,
            advance.ExchangeRate, advance.ExchangeRateDate, advance.ExchangeRateType, advance.ExchangeRateSource,
            $"Customer advance / {advance.AdvanceNumber}",
            advance.CustomerId, null, null, null, null, null, null, applicationId));

        // Cr Customer Receivable (the customer's sub-ledger GL account).
        journal.AddLine(JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journal.Id, 2, customer.AccountId,
            0m, targetBaseAmount,
            advance.CurrencyId, advance.CurrencyCodeSnapshot, advance.CurrencyDecimalPlacesSnapshot,
            0m, amount,
            advance.ExchangeRate, advance.ExchangeRateDate, advance.ExchangeRateType, advance.ExchangeRateSource,
            $"Customer receivable / {invoice.InvoiceCode}",
            advance.CustomerId, null, null, null, null, null, null, applicationId));

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
            throw new ConflictException("customer_advance_posting_profile_missing", "لم يتم إعداد Posting Profile فعال لتطبيق عربون العميل.");
        if (activeProfiles.Count > 1)
            throw new ConflictException("customer_advance_posting_profile_duplicate", "يوجد أكثر من Posting Profile فعال لتطبيق عربون العميل.");

        var profileId = activeProfiles[0].Id;
        var lineSpec = new Specification<PostingProfileLine>()
            .Where(x => x.PostingProfileId == profileId && x.AccountRole == CustomerAdvancesRole);
        var roleLines = await profileLines.ListAsync(lineSpec, cancellationToken);
        if (roleLines.Count == 0)
            throw new ConflictException("customer_advance_posting_role_missing", "Posting Profile لا يحتوي دور الحساب CustomerAdvances.");
        if (roleLines.Count > 1)
            throw new ConflictException("customer_advance_posting_role_duplicate", "يجب تعريف حساب واحد فقط لدور CustomerAdvances.");

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
