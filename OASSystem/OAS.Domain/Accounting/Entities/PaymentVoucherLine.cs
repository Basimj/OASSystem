using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class PaymentVoucherLine : AuditableEntity<Guid>
{
    private PaymentVoucherLine()
    {
    }

    private PaymentVoucherLine(
        Guid id,
        Guid paymentVoucherId,
        int lineNumber,
        Guid accountId,
        decimal amount,
        string? referenceType,
        Guid? referenceId,
        string? description)
    {
        Id = id;
        PaymentVoucherId = paymentVoucherId;
        LineNumber = lineNumber;
        AccountId = accountId;
        Amount = amount;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Description = description;
    }

    public Guid PaymentVoucherId { get; private set; }

    public int LineNumber { get; private set; }

    public Guid AccountId { get; private set; }

    public decimal Amount { get; private set; }

    public string? ReferenceType { get; private set; }

    public Guid? ReferenceId { get; private set; }

    public string? Description { get; private set; }

    public static PaymentVoucherLine Create(
        Guid id,
        Guid paymentVoucherId,
        int lineNumber,
        Guid accountId,
        decimal amount,
        string? referenceType,
        Guid? referenceId,
        string? description)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (paymentVoucherId == Guid.Empty)
            throw new ArgumentException(
                "Payment voucher id is required.",
                nameof(paymentVoucherId));

        if (lineNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineNumber));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Amount must be greater than zero.");

        return new PaymentVoucherLine(
            id,
            paymentVoucherId,
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
