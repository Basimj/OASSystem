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

        if (lines.Count == 0)
        {
            throw new ConflictException(
                "receipt_voucher_lines_required",
                "A receipt voucher must contain at least one line before posting.");
        }

        var settlementAccountId = await ResolveSettlementAccountIdAsync(
            voucher.CashAccountId,
            voucher.BankAccountId,
            cancellationToken);

        await EnsurePostingAccountAsync(settlementAccountId, cancellationToken);
        foreach (var line in lines)
        {
            await EnsurePostingAccountAsync(line.AccountId, cancellationToken);
        }

        var journal = await CreateAutomaticJournalAsync(
            voucher.VoucherDate,
            voucher.VoucherDate,
            BuildDescription("Receipt voucher", voucher.VoucherNumber, voucher.Description),
            "ReceiptVoucher",
            voucher.Id,
            cancellationToken);

        journal.AddLine(JournalEntryLine.Create(
            Guid.NewGuid(), journal.Id, 1, settlementAccountId,
            voucher.TotalAmount, 0m, voucher.Description,
            voucher.CustomerId, null, null, null, null));

        var lineNumber = 2;
        foreach (var sourceLine in lines.OrderBy(x => x.LineNumber))
        {
            journal.AddLine(JournalEntryLine.Create(
                Guid.NewGuid(), journal.Id, lineNumber++, sourceLine.AccountId,
                0m, sourceLine.Amount, sourceLine.Description,
                voucher.CustomerId, null, null, null, null));
        }

        CompleteAutomaticPosting(journal, postedBy, postedAtUtc);
        await journalRepository.AddAsync(journal, cancellationToken);
        return journal.Id;
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

        if (lines.Count == 0)
        {
            throw new ConflictException(
                "payment_voucher_lines_required",
                "A payment voucher must contain at least one line before posting.");
        }

        var settlementAccountId = await ResolveSettlementAccountIdAsync(
            voucher.CashAccountId,
            voucher.BankAccountId,
            cancellationToken);

        await EnsurePostingAccountAsync(settlementAccountId, cancellationToken);
        foreach (var line in lines)
        {
            await EnsurePostingAccountAsync(line.AccountId, cancellationToken);
        }

        var journal = await CreateAutomaticJournalAsync(
            voucher.VoucherDate,
            voucher.VoucherDate,
            BuildDescription("Payment voucher", voucher.VoucherNumber, voucher.Description),
            "PaymentVoucher",
            voucher.Id,
            cancellationToken);

        var lineNumber = 1;
        foreach (var sourceLine in lines.OrderBy(x => x.LineNumber))
        {
            journal.AddLine(JournalEntryLine.Create(
                Guid.NewGuid(), journal.Id, lineNumber++, sourceLine.AccountId,
                sourceLine.Amount, 0m, sourceLine.Description,
                null, voucher.SupplierId, null, null, null));
        }

        journal.AddLine(JournalEntryLine.Create(
            Guid.NewGuid(), journal.Id, lineNumber, settlementAccountId,
            0m, voucher.TotalAmount, voucher.Description,
            null, voucher.SupplierId, null, null, null));

        CompleteAutomaticPosting(journal, postedBy, postedAtUtc);
        await journalRepository.AddAsync(journal, cancellationToken);
        return journal.Id;
    }

    public async Task<Guid> PostExpenseAsync(
        Expense expense,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expense);

        var settlementAccountId = await ResolveSettlementAccountIdAsync(
            expense.CashAccountId,
            expense.BankAccountId,
            cancellationToken);

        await EnsurePostingAccountAsync(expense.ExpenseAccountId, cancellationToken);
        await EnsurePostingAccountAsync(settlementAccountId, cancellationToken);

        var journal = await CreateAutomaticJournalAsync(
            expense.ExpenseDate,
            expense.ExpenseDate,
            BuildDescription("Expense", expense.ExpenseNumber, expense.Description),
            "Expense",
            expense.Id,
            cancellationToken);

        journal.AddLine(JournalEntryLine.Create(
            Guid.NewGuid(), journal.Id, 1, expense.ExpenseAccountId,
            expense.Amount, 0m, expense.Description,
            null, null, null, null, null));

        journal.AddLine(JournalEntryLine.Create(
            Guid.NewGuid(), journal.Id, 2, settlementAccountId,
            0m, expense.Amount, expense.Description,
            null, null, null, null, null));

        CompleteAutomaticPosting(journal, postedBy, postedAtUtc);
        await journalRepository.AddAsync(journal, cancellationToken);
        return journal.Id;
    }

    private async Task<JournalEntry> CreateAutomaticJournalAsync(
        DateOnly postingDate,
        DateOnly documentDate,
        string description,
        string sourceDocumentType,
        Guid sourceDocumentId,
        CancellationToken cancellationToken)
    {
        var period = await ResolveFiscalPeriodAsync(postingDate, cancellationToken);
        var sequence = await sequenceNumberGenerator.NextAsync(
            $"JournalEntry-{postingDate.Year}", cancellationToken);
        var journalNumber = $"JV-{postingDate.Year:0000}-{sequence:000000}";

        return JournalEntry.Create(
            Guid.NewGuid(),
            journalNumber,
            JournalType.Automatic,
            postingDate,
            documentDate,
            period.Id,
            description,
            "Accounting",
            sourceDocumentType,
            sourceDocumentId,
            JournalEntryStatus.Draft);
    }

    private async Task<FiscalPeriod> ResolveFiscalPeriodAsync(
        DateOnly postingDate,
        CancellationToken cancellationToken)
    {
        var specification = new Specification<FiscalPeriod>()
            .Where(x => x.StartDate <= postingDate && x.EndDate >= postingDate)
            .ApplyPaging(0, 2);

        var periods = await fiscalPeriodRepository.ListAsync(specification, cancellationToken);
        if (periods.Count == 0)
        {
            throw new ConflictException(
                "fiscal_period_not_found",
                $"No fiscal period contains posting date {postingDate:yyyy-MM-dd}.");
        }

        if (periods.Count > 1)
        {
            throw new ConflictException(
                "fiscal_period_overlap",
                $"More than one fiscal period contains posting date {postingDate:yyyy-MM-dd}.");
        }

        var period = periods[0];
        if (!period.CanPostAccounting())
        {
            throw new ConflictException(
                "fiscal_period_closed",
                "Cannot post into a closed or accounting-locked fiscal period.");
        }

        return period;
    }

    private async Task<Guid> ResolveSettlementAccountIdAsync(
        Guid? cashAccountId,
        Guid? bankAccountId,
        CancellationToken cancellationToken)
    {
        if (cashAccountId.HasValue == bankAccountId.HasValue)
        {
            throw new ConflictException(
                "payment_account_required",
                "Exactly one cash account or bank account must be selected before posting.");
        }

        if (cashAccountId is Guid cashId)
        {
            var cash = await cashAccountRepository.GetByIdAsync(cashId, cancellationToken)
                ?? throw new NotFoundException(nameof(CashAccount), cashId);

            if (!cash.IsActive)
            {
                throw new ConflictException(
                    "cash_account_inactive",
                    $"Cash account '{cash.Code}' is inactive.");
            }

            return cash.AccountId;
        }

        var bankId = bankAccountId!.Value;
        var bank = await bankAccountRepository.GetByIdAsync(bankId, cancellationToken)
            ?? throw new NotFoundException(nameof(BankAccount), bankId);

        if (!bank.IsActive)
        {
            throw new ConflictException(
                "bank_account_inactive",
                $"Bank account '{bank.Code}' is inactive.");
        }

        return bank.AccountId;
    }

    private async Task EnsurePostingAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

        if (!account.IsActive)
        {
            throw new ConflictException(
                "account_inactive",
                $"Cannot post to inactive account '{account.Code}'.");
        }

        if (account.AccountType == AccountType.Header || !account.IsPostingAccount)
        {
            throw new ConflictException(
                "account_not_postable",
                $"Cannot post to header or non-posting account '{account.Code}'.");
        }
    }

    private static void CompleteAutomaticPosting(
        JournalEntry journal,
        Guid userId,
        DateTime nowUtc)
    {
        journal.SetPendingApproval();
        journal.Approve(userId, nowUtc);
        journal.Post(userId, nowUtc);
    }

    private static string BuildDescription(string documentName, string number, string? description)
    {
        var prefix = $"{documentName} {number}";
        return string.IsNullOrWhiteSpace(description)
            ? prefix
            : $"{prefix} - {description.Trim()}";
    }
}
