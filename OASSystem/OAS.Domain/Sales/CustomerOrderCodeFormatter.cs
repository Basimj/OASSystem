namespace OAS.Domain.Sales;

public static class CustomerOrderCodeFormatter
{
    public static string Format(long sequence, int year)
    {
        if (sequence <= 0)
            throw new ArgumentOutOfRangeException(nameof(sequence));
        if (year is < 1 or > 9999)
            throw new ArgumentOutOfRangeException(nameof(year));
        return $"SO-{year:0000}-{sequence:000000}";
    }

    public static string Format(long sequence, DateOnly documentDate) =>
        Format(sequence, documentDate.Year);
}
