namespace OAS.Domain.Accounting;

public static class CustomerCodeFormatter
{
    public static string Format(long sequence)
    {
        if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
        return $"CUS-{sequence:000000}";
    }
}
