using System.IO;
using System.Text.Json;

namespace OAS.Print.Desktop.Services;

public static class SampleDataStore
{
    public static JsonElement Load(string documentType)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "SampleData", documentType + ".json");
        if (!File.Exists(path))
            return JsonDocument.Parse("{}").RootElement.Clone();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }
}
