using OAS.Domain.Accounting.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.Rules;

public static class JournalPostingRules
{
    public static void EnsureBalanced(IEnumerable<JournalEntryLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var materialized = lines.ToArray();
        if (materialized.Length == 0)
            throw new DomainException("A journal entry must contain at least one line.");

        if (materialized.Sum(x => x.DebitAmount) != materialized.Sum(x => x.CreditAmount))
            throw new DomainException("Journal entry debit and credit totals must be equal.");
    }

    public static void EnsureCurrencySnapshots(JournalEntry journal, IEnumerable<JournalEntryLine> lines)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(lines);

        var materialized = lines.ToArray();
        var journalHasAnySnapshot =
            journal.BaseCurrencyId.HasValue ||
            !string.IsNullOrWhiteSpace(journal.BaseCurrencyCodeSnapshot) ||
            journal.BaseCurrencyDecimalPlacesSnapshot.HasValue;
        var linesHaveAnySnapshot = materialized.Any(HasAnyTransactionSnapshot);

        // Compatibility for historical base-currency-only journals created before
        // currency snapshots were introduced. New posting flows always set snapshots.
        if (!journalHasAnySnapshot && !linesHaveAnySnapshot)
            return;

        if (!journal.BaseCurrencyId.HasValue ||
            string.IsNullOrWhiteSpace(journal.BaseCurrencyCodeSnapshot) ||
            !journal.BaseCurrencyDecimalPlacesSnapshot.HasValue ||
            journal.BaseCurrencyDecimalPlacesSnapshot.Value > 6)
        {
            throw new DomainException("Journal base-currency snapshot is incomplete or invalid.");
        }

        foreach (var line in materialized)
        {
            if (!HasAnyTransactionSnapshot(line))
                continue; // Explicit base-currency-only line.

            if (!line.TransactionCurrencyId.HasValue ||
                string.IsNullOrWhiteSpace(line.TransactionCurrencyCodeSnapshot) ||
                !line.TransactionCurrencyDecimalPlacesSnapshot.HasValue ||
                line.TransactionCurrencyDecimalPlacesSnapshot.Value > 6 ||
                !line.TransactionDebitAmount.HasValue ||
                !line.TransactionCreditAmount.HasValue ||
                !line.ExchangeRate.HasValue ||
                line.ExchangeRate.Value <= 0 ||
                !line.ExchangeRateDate.HasValue ||
                !line.ExchangeRateType.HasValue ||
                !line.ExchangeRateSource.HasValue)
            {
                throw new DomainException("Journal line currency snapshot is incomplete or invalid.");
            }

            var txDebit = line.TransactionDebitAmount.Value;
            var txCredit = line.TransactionCreditAmount.Value;
            if ((line.DebitAmount > 0) != (txDebit > 0) ||
                (line.CreditAmount > 0) != (txCredit > 0))
            {
                throw new DomainException("Transaction and base currency amounts must use the same debit/credit direction.");
            }
        }
    }

    public static void EnsureAccountCanReceivePosting(Account account, bool isManual)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (!account.CanReceivePosting(isManual))
            throw new DomainException(isManual
                ? "The account cannot receive manual posting."
                : "The account cannot receive posting.");
    }

    private static bool HasAnyTransactionSnapshot(JournalEntryLine line) =>
        line.TransactionCurrencyId.HasValue ||
        !string.IsNullOrWhiteSpace(line.TransactionCurrencyCodeSnapshot) ||
        line.TransactionCurrencyDecimalPlacesSnapshot.HasValue ||
        line.TransactionDebitAmount.HasValue ||
        line.TransactionCreditAmount.HasValue ||
        line.ExchangeRate.HasValue ||
        line.ExchangeRateDate.HasValue ||
        line.ExchangeRateType.HasValue ||
        line.ExchangeRateSource.HasValue;
}
