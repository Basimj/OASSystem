namespace OAS.Domain.Purchasing.Formatting;
public static class PurchaseRequestCodeFormatter
{
    public static string Format(long sequence, int year) => FormatCore("PRQ", sequence, year);
    public static string Format(long sequence, DateOnly documentDate) => Format(sequence, documentDate.Year);
    private static string FormatCore(string prefix,long sequence,int year){if(sequence<=0)throw new ArgumentOutOfRangeException(nameof(sequence));if(year is <1 or >9999)throw new ArgumentOutOfRangeException(nameof(year));return $"{prefix}-{year:0000}-{sequence:000000}";}
}
