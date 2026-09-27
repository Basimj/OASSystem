namespace OAS.Printing.Core.Models;

public sealed class TemplateDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "TPL-001";
    public string Name { get; set; } = "قالب جديد";
    public string DocumentType { get; set; } = "ReceiptVoucher";
    public double PaperWidthMm { get; set; } = 148;
    public double PaperHeightMm { get; set; } = 210;
    public bool Landscape { get; set; }
    public bool IsDefault { get; set; }
    public int Version { get; set; } = 1;
    public List<TemplateElement> Elements { get; set; } = [];
}
