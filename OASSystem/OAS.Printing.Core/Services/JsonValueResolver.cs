using System.Globalization;
using System.Text.Json;

namespace OAS.Printing.Core.Services;

public static class JsonValueResolver
{
    public static JsonElement? ResolveElement(JsonElement root, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return root;

        var current = root;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current.ValueKind != JsonValueKind.Object)
                return null;

            if (!TryGetPropertyIgnoreCase(current, part, out var next))
                return null;

            current = next;
        }

        return current;
    }

    public static string ResolveText(JsonElement root, string? path, string? format = null)
    {
        var value = ResolveElement(root, path);
        if (value is null)
            return string.Empty;

        return Format(value.Value, format);
    }

    public static string Format(JsonElement value, string? format)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(format))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                return number.ToString(format, CultureInfo.CurrentCulture);

            if (value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out var date))
                return date.ToString(format, CultureInfo.CurrentCulture);
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "نعم",
            JsonValueKind.False => "لا",
            _ => value.GetRawText()
        };
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
            return true;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
