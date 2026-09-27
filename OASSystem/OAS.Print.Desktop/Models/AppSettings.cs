namespace OAS.Print.Desktop.Models;

public sealed class AppSettings
{
    public string ApiBaseUrl { get; set; } = "https://localhost:7245";
    public string WorkstationCode { get; set; } = "DEFAULT";
    public string ApiKey { get; set; } = string.Empty;
    public bool PollEnabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 3;
    public string DefaultPrinterName { get; set; } = string.Empty;
    public string OperatorName { get; set; } = Environment.UserName;
    public Dictionary<string, string> PrinterBindings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
