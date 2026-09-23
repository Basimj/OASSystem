namespace OAS.Domain.Accounting;

public static class SupplierCodeFormatter
{
    public static string Format(long sequence)
    {
        if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
        return $"SUP-{sequence:000000}";
    }
}
