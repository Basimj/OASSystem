using System.Text.Json;

namespace OAS.Printing.Core.Models;

public sealed class PrintJob
{
    public Guid JobId { get; set; } = Guid.NewGuid();
    public string DocumentType { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }
    public string? PrinterName { get; set; }
    public int Copies { get; set; } = 1;
    public bool ShowPreview { get; set; }
    public string? RequestedBy { get; set; }
    public JsonElement Data { get; set; }
}
