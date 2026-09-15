using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class JournalEntryLine : Entity<Guid>
{
    private JournalEntryLine()
    {
    }

    private JournalEntryLine(
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
        Id = id;
        JournalEntryId = journalEntryId;
        LineNumber = lineNumber;
        AccountId = accountId;
        DebitAmount = debitAmount;
        CreditAmount = creditAmount;
        Description = description;
        CustomerId = customerId;
        SupplierId = supplierId;
        CostCenterId = costCenterId;
        ProductVariantId = productVariantId;
        WarehouseId = warehouseId;
    }

    public Guid JournalEntryId { get; private set; }

    public int LineNumber { get; private set; }

    public Guid AccountId { get; private set; }

    public decimal DebitAmount { get; private set; }

    public decimal CreditAmount { get; private set; }

    public string? Description { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid? SupplierId { get; private set; }

    public Guid? CostCenterId { get; private set; }

    public Guid? ProductVariantId { get; private set; }

    public Guid? WarehouseId { get; private set; }

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
        if (id == Guid.Empty)
            throw new ArgumentException("Line id is required.", nameof(id));

        if (journalEntryId == Guid.Empty)
            throw new ArgumentException(
                "Journal entry id is required.",
                nameof(journalEntryId));

        if (lineNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineNumber));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        if (debitAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(debitAmount));

        if (creditAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(creditAmount));

        if (debitAmount > 0 && creditAmount > 0)
            throw new InvalidOperationException(
                "A journal line cannot contain both debit and credit.");

        if (debitAmount == 0 && creditAmount == 0)
            throw new InvalidOperationException(
                "A journal line must contain either debit or credit.");

        return new JournalEntryLine(
            id,
            journalEntryId,
            lineNumber,
            accountId,
            debitAmount,
            creditAmount,
            Normalize(description),
            customerId,
            supplierId,
            costCenterId,
            productVariantId,
            warehouseId);
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
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        if (debitAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(debitAmount));

        if (creditAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(creditAmount));

        if (debitAmount > 0 && creditAmount > 0)
            throw new InvalidOperationException(
                "A journal line cannot contain both debit and credit.");

        if (debitAmount == 0 && creditAmount == 0)
            throw new InvalidOperationException(
                "A journal line must contain either debit or credit.");

        AccountId = accountId;
        DebitAmount = debitAmount;
        CreditAmount = creditAmount;
        Description = Normalize(description);
        CustomerId = customerId;
        SupplierId = supplierId;
        CostCenterId = costCenterId;
        ProductVariantId = productVariantId;
        WarehouseId = warehouseId;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}