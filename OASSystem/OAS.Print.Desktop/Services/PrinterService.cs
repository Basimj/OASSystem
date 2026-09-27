using System.Printing;
using System.Windows.Documents;

namespace OAS.Print.Desktop.Services;

public sealed class PrinterService
{
    public IReadOnlyList<string> GetPrinters()
    {
        using var server = new LocalPrintServer();
        return server.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections })
            .Select(x => x.FullName)
            .OrderBy(x => x)
            .ToArray();
    }

    public void Print(FixedDocument document, string printerName, int copies = 1)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            throw new InvalidOperationException("لم يتم اختيار طابعة.");

        using var server = new LocalPrintServer();
        var queue = server.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections })
            .FirstOrDefault(x => string.Equals(x.FullName, printerName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"الطابعة غير موجودة: {printerName}");

        var writer = PrintQueue.CreateXpsDocumentWriter(queue);
        for (var i = 0; i < Math.Max(1, copies); i++)
            writer.Write(document.DocumentPaginator);
    }
}
