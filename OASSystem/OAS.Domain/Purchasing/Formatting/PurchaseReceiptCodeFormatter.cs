namespace OAS.Domain.Purchasing.Formatting;
public static class PurchaseReceiptCodeFormatter
{
    public static string Format(long sequence, int year){if(sequence<=0)throw new ArgumentOutOfRangeException(nameof(sequence));if(year is <1 or >9999)throw new ArgumentOutOfRangeException(nameof(year));return $"GR-{year:0000}-{sequence:000000}";}
    public static string Format(long sequence, DateOnly documentDate)=>Format(sequence,documentDate.Year);
}
