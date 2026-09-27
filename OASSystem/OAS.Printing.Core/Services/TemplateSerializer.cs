using System.Text.Json;
using System.Text.Json.Serialization;
using OAS.Printing.Core.Models;

namespace OAS.Printing.Core.Services;

public static class TemplateSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(TemplateDefinition template) =>
        JsonSerializer.Serialize(template, Options);

    public static TemplateDefinition Deserialize(string json) =>
        JsonSerializer.Deserialize<TemplateDefinition>(json, Options)
        ?? throw new InvalidOperationException("تعذر قراءة ملف القالب.");
}
