using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Posting;

public sealed class AccountingDocumentPostingService(
    IRepository<JournalEntry, Guid> journalRepository,
    IReadRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
    IReadRepository<Account, Guid> accountRepository,
    IReadRepository<CashAccount, Guid> cashAccountRepository,
    IReadRepository<BankAccount, Guid> bankAccountRepository,
    IReadRepository<AccountingSettings, Guid> settingsRepository,
    IReadRepository<Currency, Guid> currencyRepository,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IAccountingDocumentPostingService
{
    public async Task<Guid> PostReceiptVoucherAsync(
        ReceiptVoucher voucher,
        IReadOnlyCollection<ReceiptVoucherLine> lines,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0) throw new ConflictException("receipt_voucher_lines_required", "A receipt voucher must contain at least one line before posting.");

        if (IsModern(lines))
            return await PostModernReceiptAsync(voucher, lines, postedBy, postedAtUtc, cancellationToken);

        return await PostLegacyReceiptAsync(voucher, lines, postedBy, postedAtUtc, cancellationToken);
    }

    public async Task<Guid> PostPaymentVoucherAsync(
        PaymentVoucher voucher,
        IReadOnlyCollection<PaymentVoucherLine> lines,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0) throw new ConflictException("payment_voucher_lines_required", "A payment voucher must contain at least one line before posting.");

        if (IsModern(lines))
            return await PostModernPaymentAsync(voucher, lines, postedBy, postedAtUtc, cancellationToken);

        return await PostLegacyPaymentAsync(voucher, lines, postedBy, postedAtUtc, cancellationToken);
    }

    public async Task<Guid> PostExpenseAsync(
        Expense expense,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expense);
        var settlementAccountId = await ResolveLegacySettlementAccountIdAsync(expense.CashAccountId, expense.BankAccountId, cancellationToken);
        await EnsurePostingAccountAsync(expense.ExpenseAccountId, cancellationToken);
        await EnsurePostingAccountAsync(settlementAccountId, cancellationToken);
        var (settings, baseCurrency) = await GetBaseCurrencyAsync(cancellationToken);
        var journal = await CreateAutomaticJournalAsync(
            expense.ExpenseDate, expense.ExpenseDate,
            BuildDescription("Expense", expense.ExpenseNumber, expense.Description),
            "Expense", expense.Id,
            settings.BaseCurrencyId, baseCurrency.Code, baseCurrency.DecimalPlaces,
            cancellationToken);

        journal.AddLine(JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journal.Id, 1, expense.ExpenseAccountId,
            expense.Amount, 0m, baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces,
            expense.Amount, 0m, 1m, expense.ExpenseDate, ExchangeRateType.Accounting, ExchangeRateSource.System,
            expense.Description, null, null, null, null, null, null, null, expense.Id));
        journal.AddLine(JournalEntryLine.CreateMultiCurrency(
            Guid.NewGuid(), journal.Id, 2, settlementAccountId,
            0m, expense.Amount, baseCurrency.Id, baseCurrency.Code, baseCurrency.DecimalPlaces,
            0m, expense.Amount, 1m, expense.ExpenseDate, ExchangeRateType.Accounting, ExchangeRateSource.System,
            expense.Description, null, null, null, null, null, null, null, expense.Id));

        CompleteAutomaticPosting(journal, postedBy, postedAtUtc);
        await journalRepository.AddAsync(journal, cancellationToken);
        return journal.Id;
    }

    private async Task<Guid> PostModernReceiptAsync(ReceiptVoucher voucher, IReadOnlyCollection<ReceiptVoucherLine> lines, Guid userId, DateTime nowUtc, CancellationToken ct)
    {
        if (!voucher.BaseCurrencyId.HasValue || string.IsNullOrWhiteSpace(voucher.BaseCurrencyCodeSnapshot) || !voucher.BaseCurrencyDecimalPlacesSnapshot.HasValue)
            throw new ConflictException("receipt_base_currency_missing", "Receipt voucher base currency snapshot is missing.");
        var journal = await CreateAutomaticJournalAsync(voucher.VoucherDate, voucher.VoucherDate,
            BuildDescription("Receipt voucher", voucher.VoucherNumber, voucher.Description), "ReceiptVoucher", voucher.Id,
            voucher.BaseCurrencyId, voucher.BaseCurrencyCodeSnapshot, voucher.BaseCurrencyDecimalPlacesSnapshot, ct);
        var number = 1;
        foreach (var line in lines.OrderBy(x => x.LineNumber))
        {
            await EnsurePostingAccountAsync(line.CounterpartyAccountId!.Value, ct);
            await EnsurePostingAccountAsync(line.SettlementAccountId!.Value, ct);
            var baseAmount = line.BaseAmount!.Value;
            journal.AddLine(CreateJournalLine(journal.Id, number++, line.SettlementAccountId.Value, baseAmount, 0m, line.Amount, 0m,
                line.CurrencyId!.Value, line.CurrencyCodeSnapshot!, line.CurrencyDecimalPlacesSnapshot!.Value, line.ExchangeRate!.Value,
                line.ExchangeRateDate!.Value, line.ExchangeRateType!.Value, line.ExchangeRateSource!.Value, line, line.Description));
            journal.AddLine(CreateJournalLine(journal.Id, number++, line.CounterpartyAccountId.Value, 0m, baseAmount, 0m, line.Amount,
                line.CurrencyId.Value, line.CurrencyCodeSnapshot!, line.CurrencyDecimalPlacesSnapshot.Value, line.ExchangeRate.Value,
                line.ExchangeRateDate.Value, line.ExchangeRateType.Value, line.ExchangeRateSource.Value, line, line.Description));
        }
        CompleteAutomaticPosting(journal, userId, nowUtc);
        await journalRepository.AddAsync(journal, ct);
        return journal.Id;
    }

    private async Task<Guid> PostModernPaymentAsync(PaymentVoucher voucher, IReadOnlyCollection<PaymentVoucherLine> lines, Guid userId, DateTime nowUtc, CancellationToken ct)
    {
        if (!voucher.BaseCurrencyId.HasValue || string.IsNullOrWhiteSpace(voucher.BaseCurrencyCodeSnapshot) || !voucher.BaseCurrencyDecimalPlacesSnapshot.HasValue)
            throw new ConflictException("payment_base_currency_missing", "Payment voucher base currency snapshot is missing.");
        var journal = await CreateAutomaticJournalAsync(voucher.VoucherDate, voucher.VoucherDate,
            BuildDescription("Payment voucher", voucher.VoucherNumber, voucher.Description), "PaymentVoucher", voucher.Id,
            voucher.BaseCurrencyId, voucher.BaseCurrencyCodeSnapshot, voucher.BaseCurrencyDecimalPlacesSnapshot, ct);
        var number = 1;
        foreach (var line in lines.OrderBy(x => x.LineNumber))
        {
            await EnsurePostingAccountAsync(line.CounterpartyAccountId!.Value, ct);
            await EnsurePostingAccountAsync(line.SettlementAccountId!.Value, ct);
            var baseAmount = line.BaseAmount!.Value;
            journal.AddLine(CreateJournalLine(journal.Id, number++, line.CounterpartyAccountId.Value, baseAmount, 0m, line.Amount, 0m,
                line.CurrencyId!.Value, line.CurrencyCodeSnapshot!, line.CurrencyDecimalPlacesSnapshot!.Value, line.ExchangeRate!.Value,
                line.ExchangeRateDate!.Value, line.ExchangeRateType!.Value, line.ExchangeRateSource!.Value, line, line.Description));
            journal.AddLine(CreateJournalLine(journal.Id, number++, line.SettlementAccountId.Value, 0m, baseAmount, 0m, line.Amount,
                line.CurrencyId.Value, line.CurrencyCodeSnapshot!, line.CurrencyDecimalPlacesSnapshot.Value, line.ExchangeRate.Value,
                line.ExchangeRateDate.Value, line.ExchangeRateType.Value, line.ExchangeRateSource.Value, line, line.Description));
        }
        CompleteAutomaticPosting(journal, userId, nowUtc);
        await journalRepository.AddAsync(journal, ct);
        return journal.Id;
    }

    private static JournalEntryLine CreateJournalLine(
        Guid journalId, int lineNumber, Guid accountId,
        decimal debit, decimal credit, decimal txDebit, decimal txCredit,
        Guid currencyId, string currencyCode, byte decimals, decimal rate,
        DateOnly rateDate, ExchangeRateType rateType, ExchangeRateSource rateSource,
        ReceiptVoucherLine source, string? description) =>
        JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), journalId, lineNumber, accountId, debit, credit,
            currencyId, currencyCode, decimals, txDebit, txCredit, rate, rateDate, rateType, rateSource,
            description, source.CustomerId, source.SupplierId, source.EmployeeId, source.PartyNameSnapshot,
            null, null, null, source.Id);

    private static JournalEntryLine CreateJournalLine(
        Guid journalId, int lineNumber, Guid accountId,
        decimal debit, decimal credit, decimal txDebit, decimal txCredit,
        Guid currencyId, string currencyCode, byte decimals, decimal rate,
        DateOnly rateDate, ExchangeRateType rateType, ExchangeRateSource rateSource,
        PaymentVoucherLine source, string? description) =>
        JournalEntryLine.CreateMultiCurrency(Guid.NewGuid(), journalId, lineNumber, accountId, debit, credit,
            currencyId, currencyCode, decimals, txDebit, txCredit, rate, rateDate, rateType, rateSource,
            description, source.CustomerId, source.SupplierId, source.EmployeeId, source.PartyNameSnapshot,
            null, null, null, source.Id);

    private async Task<Guid> PostLegacyReceiptAsync(ReceiptVoucher voucher, IReadOnlyCollection<ReceiptVoucherLine> lines, Guid userId, DateTime nowUtc, CancellationToken ct)
    {
        var settlementId = await ResolveLegacySettlementAccountIdAsync(voucher.CashAccountId, voucher.BankAccountId, ct);
        await EnsurePostingAccountAsync(settlementId, ct);
        foreach (var line in lines) await EnsurePostingAccountAsync(line.AccountId, ct);
        var journal = await CreateAutomaticJournalAsync(voucher.VoucherDate, voucher.VoucherDate,
            BuildDescription("Receipt voucher", voucher.VoucherNumber, voucher.Description), "ReceiptVoucher", voucher.Id, null, null, null, ct);
        journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, 1, settlementId, voucher.TotalAmount, 0m, voucher.Description, voucher.CustomerId, null, null, null, null));
        var n = 2;
        foreach (var line in lines.OrderBy(x => x.LineNumber)) journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, n++, line.AccountId, 0m, line.Amount, line.Description, voucher.CustomerId, null, null, null, null));
        CompleteAutomaticPosting(journal, userId, nowUtc); await journalRepository.AddAsync(journal, ct); return journal.Id;
    }

    private async Task<Guid> PostLegacyPaymentAsync(PaymentVoucher voucher, IReadOnlyCollection<PaymentVoucherLine> lines, Guid userId, DateTime nowUtc, CancellationToken ct)
    {
        var settlementId = await ResolveLegacySettlementAccountIdAsync(voucher.CashAccountId, voucher.BankAccountId, ct);
        await EnsurePostingAccountAsync(settlementId, ct);
        foreach (var line in lines) await EnsurePostingAccountAsync(line.AccountId, ct);
        var journal = await CreateAutomaticJournalAsync(voucher.VoucherDate, voucher.VoucherDate,
            BuildDescription("Payment voucher", voucher.VoucherNumber, voucher.Description), "PaymentVoucher", voucher.Id, null, null, null, ct);
        var n = 1;
        foreach (var line in lines.OrderBy(x => x.LineNumber)) journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, n++, line.AccountId, line.Amount, 0m, line.Description, null, voucher.SupplierId, null, null, null));
        journal.AddLine(JournalEntryLine.Create(Guid.NewGuid(), journal.Id, n, settlementId, 0m, voucher.TotalAmount, voucher.Description, null, voucher.SupplierId, null, null, null));
        CompleteAutomaticPosting(journal, userId, nowUtc); await journalRepository.AddAsync(journal, ct); return journal.Id;
    }

    private async Task<JournalEntry> CreateAutomaticJournalAsync(
        DateOnly postingDate, DateOnly documentDate, string description, string sourceDocumentType, Guid sourceDocumentId,
        Guid? baseCurrencyId, string? baseCurrencyCode, byte? baseCurrencyDecimals, CancellationToken ct)
    {
        var period = await ResolveFiscalPeriodAsync(postingDate, ct);
        var sequence = await sequenceNumberGenerator.NextAsync($"JournalEntry-{postingDate.Year}", ct);
        var journal = JournalEntry.Create(Guid.NewGuid(), $"JV-{postingDate.Year:0000}-{sequence:000000}", JournalType.Automatic,
            postingDate, documentDate, period.Id, description, "Accounting", sourceDocumentType, sourceDocumentId, JournalEntryStatus.Draft);
        if (baseCurrencyId.HasValue && !string.IsNullOrWhiteSpace(baseCurrencyCode) && baseCurrencyDecimals.HasValue)
            journal.SetBaseCurrencySnapshot(baseCurrencyId.Value, baseCurrencyCode, baseCurrencyDecimals.Value);
        return journal;
    }

    private async Task<FiscalPeriod> ResolveFiscalPeriodAsync(DateOnly postingDate, CancellationToken ct)
    {
        var periods = await fiscalPeriodRepository.ListAsync(new Specification<FiscalPeriod>().Where(x => x.StartDate <= postingDate && x.EndDate >= postingDate).ApplyPaging(0, 2), ct);
        if (periods.Count == 0) throw new ConflictException("fiscal_period_not_found", $"No fiscal period contains posting date {postingDate:yyyy-MM-dd}.");
        if (periods.Count > 1) throw new ConflictException("fiscal_period_overlap", $"More than one fiscal period contains posting date {postingDate:yyyy-MM-dd}.");
        if (!periods[0].CanPostAccounting()) throw new ConflictException("fiscal_period_closed", "Cannot post into a closed or accounting-locked fiscal period.");
        return periods[0];
    }

    private async Task<Guid> ResolveLegacySettlementAccountIdAsync(Guid? cashAccountId, Guid? bankAccountId, CancellationToken ct)
    {
        if (cashAccountId.HasValue == bankAccountId.HasValue) throw new ConflictException("payment_account_required", "Exactly one cash account or bank account must be selected before posting.");
        if (cashAccountId is Guid cashId){var cash=await cashAccountRepository.GetByIdAsync(cashId,ct)??throw new NotFoundException(nameof(CashAccount),cashId);if(!cash.IsActive)throw new ConflictException("cash_account_inactive",$"Cash account '{cash.Code}' is inactive.");return cash.AccountId;}
        var bankId=bankAccountId!.Value;var bank=await bankAccountRepository.GetByIdAsync(bankId,ct)??throw new NotFoundException(nameof(BankAccount),bankId);if(!bank.IsActive)throw new ConflictException("bank_account_inactive",$"Bank account '{bank.Code}' is inactive.");return bank.AccountId;
    }

    private async Task EnsurePostingAccountAsync(Guid accountId, CancellationToken ct)
    {
        var account=await accountRepository.GetByIdAsync(accountId,ct)??throw new NotFoundException(nameof(Account),accountId);
        if(!account.IsActive)throw new ConflictException("account_inactive",$"Cannot post to inactive account '{account.Code}'.");
        if(account.AccountType==AccountType.Header||!account.IsPostingAccount)throw new ConflictException("account_not_postable",$"Cannot post to header or non-posting account '{account.Code}'.");
    }

    private async Task<(AccountingSettings Settings, Currency BaseCurrency)> GetBaseCurrencyAsync(CancellationToken ct)
    {
        var settings=await settingsRepository.GetByIdAsync(AccountingSettings.SingletonId,ct)??throw new ConflictException("accounting_settings_required","Accounting settings and base currency must be configured first.");
        var currency=await currencyRepository.GetByIdAsync(settings.BaseCurrencyId,ct)??throw new ConflictException("base_currency_missing","Configured base currency does not exist.");
        return (settings,currency);
    }

    private static bool IsModern(IEnumerable<ReceiptVoucherLine> lines)=>lines.All(x=>x.CounterpartyAccountId.HasValue&&x.SettlementAccountId.HasValue&&x.CurrencyId.HasValue&&x.BaseAmount.HasValue&&x.ExchangeRate.HasValue&&x.ExchangeRateDate.HasValue&&x.ExchangeRateType.HasValue&&x.ExchangeRateSource.HasValue&&!string.IsNullOrWhiteSpace(x.CurrencyCodeSnapshot)&&x.CurrencyDecimalPlacesSnapshot.HasValue);
    private static bool IsModern(IEnumerable<PaymentVoucherLine> lines)=>lines.All(x=>x.CounterpartyAccountId.HasValue&&x.SettlementAccountId.HasValue&&x.CurrencyId.HasValue&&x.BaseAmount.HasValue&&x.ExchangeRate.HasValue&&x.ExchangeRateDate.HasValue&&x.ExchangeRateType.HasValue&&x.ExchangeRateSource.HasValue&&!string.IsNullOrWhiteSpace(x.CurrencyCodeSnapshot)&&x.CurrencyDecimalPlacesSnapshot.HasValue);
    private static void CompleteAutomaticPosting(JournalEntry journal,Guid userId,DateTime nowUtc){journal.SetPendingApproval();journal.Approve(userId,nowUtc);journal.Post(userId,nowUtc);}
    private static string BuildDescription(string documentName,string number,string? description){var prefix=$"{documentName} {number}";return string.IsNullOrWhiteSpace(description)?prefix:$"{prefix} - {description.Trim()}";}
}
