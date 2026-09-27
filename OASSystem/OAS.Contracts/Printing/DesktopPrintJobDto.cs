using System.Text.Json;

namespace OAS.Contracts.Printing;

public sealed record DesktopPrintJobDto(
    Guid JobId,
    string DocumentType,
    string? TemplateCode,
    string? PrinterName,
    int Copies,
    bool ShowPreview,
    JsonElement Data);
