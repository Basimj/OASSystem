namespace OAS.Printing.Core.Models;

public sealed class TemplateElement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TemplateElementType Type { get; set; } = TemplateElementType.Text;
    public string Name { get; set; } = "عنصر";
    public double Xmm { get; set; } = 10;
    public double Ymm { get; set; } = 10;
    public double WidthMm { get; set; } = 50;
    public double HeightMm { get; set; } = 8;

    public string Text { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty;
    public string? Format { get; set; }
    public string Prefix { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;

    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 11;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool RightToLeft { get; set; } = true;
    public TemplateTextAlignment Alignment { get; set; } = TemplateTextAlignment.Right;

    // Presentation settings. Hex colors such as #5A2DD6 are supported.
    public string ForegroundColor { get; set; } = "#27263B";
    public string? BackgroundColor { get; set; }
    public string BorderColor { get; set; } = "#E7E5EF";
    public double BorderThickness { get; set; }
    public double CornerRadiusMm { get; set; }
    public double PaddingMm { get; set; } = 1.1;

    public double LineThickness { get; set; } = 1;
    public string? ImagePath { get; set; }

    public double RowHeightMm { get; set; } = 7;
    public int MaxRows { get; set; } = 12;
    public bool ShowHeader { get; set; } = true;
    public string HeaderBackgroundColor { get; set; } = "#5A2DD6";
    public string HeaderForegroundColor { get; set; } = "#FFFFFF";
    public string? AlternateRowBackgroundColor { get; set; } = "#FAF9FD";
    public List<TemplateTableColumn> Columns { get; set; } = [];
}
