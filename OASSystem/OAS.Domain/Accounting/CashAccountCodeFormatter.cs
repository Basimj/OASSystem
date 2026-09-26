namespace OAS.Domain.Accounting;

public static class CashAccountCodeFormatter
{
    public static string Format(long sequence)
    {
        if (sequence <= 0)
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be positive.");

        return $"CASH-{sequence:000000}";
    }
}
