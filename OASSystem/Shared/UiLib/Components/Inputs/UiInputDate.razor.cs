using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Inputs;

public partial class UiInputDate
{
    [Parameter] public string? Id { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool EnableNativeValidation { get; set; } = true;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;

    private string InputId => string.IsNullOrWhiteSpace(Id) ? $"oas-date-{FieldIdentifier.FieldName}" : Id;
    private string SizeCss => Size.ToString().ToLowerInvariant();

    private void HandleChange(ChangeEventArgs args) =>
        CurrentValueAsString = args.Value?.ToString();

    protected override string? FormatValueAsString(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    protected override bool TryParseValueFromString(string? value, out DateOnly? result, out string validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            validationErrorMessage = string.Empty;
            return true;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            result = parsed;
            validationErrorMessage = string.Empty;
            return true;
        }

        result = null;
        validationErrorMessage = "قيمة التاريخ غير صحيحة.";
        return false;
    }
}
