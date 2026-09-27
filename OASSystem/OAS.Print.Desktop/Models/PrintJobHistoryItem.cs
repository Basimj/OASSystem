namespace OAS.Print.Desktop.Models;

public sealed class PrintJobHistoryItem
{
    public Guid JobId { get; init; }
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.Now;
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentName { get; init; } = string.Empty;
    public string? DocumentNumber { get; init; }
    public string PrinterName { get; init; } = string.Empty;
    public int Copies { get; init; }
    public bool Success { get; init; }
    public string StatusText => Success ? "تمت الطباعة" : "فشلت";
    public string? Error { get; init; }
}
