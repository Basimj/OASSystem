namespace OAS.Printing.Core.Models;

public sealed class TemplateTableColumn
{
    public string Header { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty;
    public double WidthMm { get; set; } = 30;
    public string? Format { get; set; }
    public TemplateTextAlignment Alignment { get; set; } = TemplateTextAlignment.Right;
}
