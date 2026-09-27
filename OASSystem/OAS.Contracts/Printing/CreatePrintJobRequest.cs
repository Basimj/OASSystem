namespace OAS.Contracts.Printing;

public sealed record CreatePrintJobRequest(
    string DocumentType,
    Guid DocumentId,
    string WorkstationCode = "DEFAULT",
    string? TemplateCode = null,
    string? PrinterName = null,
    int Copies = 1,
    bool ShowPreview = false);
