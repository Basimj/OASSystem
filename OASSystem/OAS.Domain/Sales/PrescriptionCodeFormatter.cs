namespace OAS.Domain.Sales;

public static class PrescriptionCodeFormatter
{
    public static string Format(long sequence)
    {
        if (sequence <= 0)
            throw new ArgumentOutOfRangeException(nameof(sequence));
        return $"RX-{sequence:000000}";
    }
}
