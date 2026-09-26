using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class JournalEntryLine : AuditableEntity<Guid>
{
    private JournalEntryLine() { }

    public Guid JournalEntryId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid AccountId { get; private set; }

    // Always base-currency amounts.
    public decimal DebitAmount { get; private set; }
    public decimal CreditAmount { get; private set; }

    public Guid? TransactionCurrencyId { get; private set; }
    public string? TransactionCurrencyCodeSnapshot { get; private set; }
    public byte? TransactionCurrencyDecimalPlacesSnapshot { get; private set; }
    public decimal? TransactionDebitAmount { get; private set; }
    public decimal? TransactionCreditAmount { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public DateOnly? ExchangeRateDate { get; private set; }
    public ExchangeRateType? ExchangeRateType { get; private set; }
    public ExchangeRateSource? ExchangeRateSource { get; private set; }
    public Guid? SourceDocumentLineId { get; private set; }

    public string? Description { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public string? PartyNameSnapshot { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public Guid? WarehouseId { get; private set; }

    public static JournalEntryLine CreateMultiCurrency(
        Guid id,
        Guid journalEntryId,
        int lineNumber,
        Guid accountId,
        decimal debitAmount,
        decimal creditAmount,
        Guid transactionCurrencyId,
        string transactionCurrencyCodeSnapshot,
        byte transactionCurrencyDecimalPlacesSnapshot,
        decimal transactionDebitAmount,
        decimal transactionCreditAmount,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        ExchangeRateType exchangeRateType,
        ExchangeRateSource exchangeRateSource,
        string? description,
        Guid? customerId,
        Guid? supplierId,
        Guid? employeeId,
        string? partyNameSnapshot,
        Guid? costCenterId,
        Guid? productVariantId,
        Guid? warehouseId,
        Guid? sourceDocumentLineId)
    {
        ValidateIdentity(id, journalEntryId, lineNumber, accountId);
        ValidateAmounts(debitAmount, creditAmount);
        ValidateAmounts(transactionDebitAmount, transactionCreditAmount);
        if (transactionCurrencyId == Guid.Empty) throw new ArgumentException("Transaction currency is required.", nameof(transactionCurrencyId));
        if (string.IsNullOrWhiteSpace(transactionCurrencyCodeSnapshot)) throw new ArgumentException("Transaction currency code snapshot is required.", nameof(transactionCurrencyCodeSnapshot));
        if (transactionCurrencyDecimalPlacesSnapshot > 6) throw new ArgumentOutOfRangeException(nameof(transactionCurrencyDecimalPlacesSnapshot));
        if (exchangeRate <= 0) throw new ArgumentOutOfRangeException(nameof(exchangeRate));

        // Direction must be identical in transaction and base currencies.
        if ((debitAmount > 0) != (transactionDebitAmount > 0) ||
            (creditAmount > 0) != (transactionCreditAmount > 0))
        {
            throw new InvalidOperationException("Transaction and base amounts must use the same debit/credit direction.");
        }

        return new JournalEntryLine
        {
            Id = id,
            JournalEntryId = journalEntryId,
            LineNumber = lineNumber,
            AccountId = accountId,
            DebitAmount = debitAmount,
            CreditAmount = creditAmount,
            TransactionCurrencyId = transactionCurrencyId,
            TransactionCurrencyCodeSnapshot = transactionCurrencyCodeSnapshot.Trim().ToUpperInvariant(),
            TransactionCurrencyDecimalPlacesSnapshot = transactionCurrencyDecimalPlacesSnapshot,
            TransactionDebitAmount = transactionDebitAmount,
            TransactionCreditAmount = transactionCreditAmount,
            ExchangeRate = exchangeRate,
            ExchangeRateDate = exchangeRateDate,
            ExchangeRateType = exchangeRateType,
            ExchangeRateSource = exchangeRateSource,
            SourceDocumentLineId = Normalize(sourceDocumentLineId),
            Description = Normalize(description),
            CustomerId = Normalize(customerId),
            SupplierId = Normalize(supplierId),
            EmployeeId = Normalize(employeeId),
            PartyNameSnapshot = Normalize(partyNameSnapshot),
            CostCenterId = Normalize(costCenterId),
            ProductVariantId = Normalize(productVariantId),
            WarehouseId = Normalize(warehouseId)
        };
    }

    // Compatibility factory for historical/base-currency-only code.
    public static JournalEntryLine Create(
        Guid id,
        Guid journalEntryId,
        int lineNumber,
        Guid accountId,
        decimal debitAmount,
        decimal creditAmount,
        string? description,
        Guid? customerId,
        Guid? supplierId,
        Guid? costCenterId,
        Guid? productVariantId,
        Guid? warehouseId)
    {
        ValidateIdentity(id, journalEntryId, lineNumber, accountId);
        ValidateAmounts(debitAmount, creditAmount);
        return new JournalEntryLine
        {
            Id = id,
            JournalEntryId = journalEntryId,
            LineNumber = lineNumber,
            AccountId = accountId,
            DebitAmount = debitAmount,
            CreditAmount = creditAmount,
            Description = Normalize(description),
            CustomerId = Normalize(customerId),
            SupplierId = Normalize(supplierId),
            CostCenterId = Normalize(costCenterId),
            ProductVariantId = Normalize(productVariantId),
            WarehouseId = Normalize(warehouseId)
        };
    }

    public void UpdateMultiCurrency(
        Guid accountId,
        decimal debitAmount,
        decimal creditAmount,
        Guid transactionCurrencyId,
        string transactionCurrencyCodeSnapshot,
        byte transactionCurrencyDecimalPlacesSnapshot,
        decimal transactionDebitAmount,
        decimal transactionCreditAmount,
        decimal exchangeRate,
        DateOnly exchangeRateDate,
        ExchangeRateType exchangeRateType,
        ExchangeRateSource exchangeRateSource,
        string? description,
        Guid? customerId,
        Guid? supplierId,
        Guid? employeeId,
        string? partyNameSnapshot,
        Guid? costCenterId,
        Guid? productVariantId,
        Guid? warehouseId,
        Guid? sourceDocumentLineId)
    {
        ValidateIdentity(Id == Guid.Empty ? Guid.NewGuid() : Id, JournalEntryId, Math.Max(LineNumber, 1), accountId);
        ValidateAmounts(debitAmount, creditAmount);
        ValidateAmounts(transactionDebitAmount, transactionCreditAmount);
        if (transactionCurrencyId == Guid.Empty) throw new ArgumentException("Transaction currency is required.", nameof(transactionCurrencyId));
        if (string.IsNullOrWhiteSpace(transactionCurrencyCodeSnapshot)) throw new ArgumentException("Transaction currency snapshot is required.", nameof(transactionCurrencyCodeSnapshot));
        if (transactionCurrencyDecimalPlacesSnapshot > 6) throw new ArgumentOutOfRangeException(nameof(transactionCurrencyDecimalPlacesSnapshot));
        if (exchangeRate <= 0) throw new ArgumentOutOfRangeException(nameof(exchangeRate));
        if ((debitAmount > 0) != (transactionDebitAmount > 0) || (creditAmount > 0) != (transactionCreditAmount > 0))
            throw new InvalidOperationException("Transaction and base amounts must use the same debit/credit direction.");

        AccountId = accountId;
        DebitAmount = debitAmount;
        CreditAmount = creditAmount;
        TransactionCurrencyId = transactionCurrencyId;
        TransactionCurrencyCodeSnapshot = transactionCurrencyCodeSnapshot.Trim().ToUpperInvariant();
        TransactionCurrencyDecimalPlacesSnapshot = transactionCurrencyDecimalPlacesSnapshot;
        TransactionDebitAmount = transactionDebitAmount;
        TransactionCreditAmount = transactionCreditAmount;
        ExchangeRate = exchangeRate;
        ExchangeRateDate = exchangeRateDate;
        ExchangeRateType = exchangeRateType;
        ExchangeRateSource = exchangeRateSource;
        SourceDocumentLineId = Normalize(sourceDocumentLineId);
        Description = Normalize(description);
        CustomerId = Normalize(customerId);
        SupplierId = Normalize(supplierId);
        EmployeeId = Normalize(employeeId);
        PartyNameSnapshot = Normalize(partyNameSnapshot);
        CostCenterId = Normalize(costCenterId);
        ProductVariantId = Normalize(productVariantId);
        WarehouseId = Normalize(warehouseId);
    }

    public void Update(
        Guid accountId,
        decimal debitAmount,
        decimal creditAmount,
        string? description,
        Guid? customerId,
        Guid? supplierId,
        Guid? costCenterId,
        Guid? productVariantId,
        Guid? warehouseId)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("Account id is required.", nameof(accountId));
        ValidateAmounts(debitAmount, creditAmount);
        AccountId = accountId;
        DebitAmount = debitAmount;
        CreditAmount = creditAmount;
        Description = Normalize(description);
        CustomerId = Normalize(customerId);
        SupplierId = Normalize(supplierId);
        CostCenterId = Normalize(costCenterId);
        ProductVariantId = Normalize(productVariantId);
        WarehouseId = Normalize(warehouseId);
    }

    internal void SetLineNumber(int lineNumber)
    {
        if (lineNumber <= 0) throw new ArgumentOutOfRangeException(nameof(lineNumber));
        LineNumber = lineNumber;
    }

    private static void ValidateIdentity(Guid id, Guid journalEntryId, int lineNumber, Guid accountId)
    {
        if (id == Guid.Empty) throw new ArgumentException("Line id is required.", nameof(id));
        if (journalEntryId == Guid.Empty) throw new ArgumentException("Journal entry id is required.", nameof(journalEntryId));
        if (lineNumber <= 0) throw new ArgumentOutOfRangeException(nameof(lineNumber));
        if (accountId == Guid.Empty) throw new ArgumentException("Account id is required.", nameof(accountId));
    }

    private static void ValidateAmounts(decimal debitAmount, decimal creditAmount)
    {
        if (debitAmount < 0) throw new ArgumentOutOfRangeException(nameof(debitAmount));
        if (creditAmount < 0) throw new ArgumentOutOfRangeException(nameof(creditAmount));
        if (debitAmount > 0 && creditAmount > 0) throw new InvalidOperationException("A journal line cannot contain both debit and credit.");
        if (debitAmount == 0 && creditAmount == 0) throw new InvalidOperationException("A journal line must contain either debit or credit.");
    }

    private static Guid? Normalize(Guid? value) => value is { } id && id != Guid.Empty ? id : null;
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
