using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class ReceiptVoucherLine : Entity<Guid>
{
    private ReceiptVoucherLine()
    {
    }

    private ReceiptVoucherLine(
        Guid id,
        Guid receiptVoucherId,
        int lineNumber,
        Guid accountId,
        decimal amount,
        string? referenceType,
        Guid? referenceId,
        string? description)
    {
        Id = id;
        ReceiptVoucherId = receiptVoucherId;
        LineNumber = lineNumber;
        AccountId = accountId;
        Amount = amount;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Description = description;
    }

    public Guid ReceiptVoucherId { get; private set; }

    public int LineNumber { get; private set; }

    public Guid AccountId { get; private set; }

    public decimal Amount { get; private set; }

    public string? ReferenceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public string? Description { get; private set; }

    public static ReceiptVoucherLine Create(
        Guid id,
        Guid receiptVoucherId,
        int lineNumber,
        Guid accountId,
        decimal amount,
        string? referenceType,
        Guid? referenceId,
        string? description)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (receiptVoucherId == Guid.Empty)
            throw new ArgumentException(
                "Receipt voucher id is required.",
                nameof(receiptVoucherId));

        if (lineNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineNumber));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Amount must be greater than zero.");

        return new ReceiptVoucherLine(
            id,
            receiptVoucherId,
            lineNumber,
            accountId,
            amount,
            Normalize(referenceType),
            referenceId,
            Normalize(description));
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}