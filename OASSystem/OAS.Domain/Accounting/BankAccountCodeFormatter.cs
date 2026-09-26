namespace OAS.Domain.Accounting;

public static class BankAccountCodeFormatter
{
    public static string Format(long sequence)
    {
        if (sequence <= 0)
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be positive.");

        return $"BANK-{sequence:000000}";
    }
}
