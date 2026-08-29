using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Inputs;

public partial class UiSelect
{
    [Parameter] public string? Id { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public IReadOnlyList<UiSelectOption> Options { get; set; } = [];
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool EnableNativeValidation { get; set; } = true;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Medium;
    [Parameter] public EventCallback<string?> OnValueChanged { get; set; }

    private string InputId => string.IsNullOrWhiteSpace(Id) ? $"oas-select-{FieldIdentifier.FieldName}" : Id;
    private string InputCssClass => $"ui-select__control ui-select__control--{Size.ToString().ToLowerInvariant()} {CssClass}".Trim();

    protected override bool TryParseValueFromString(string? value, out string? result, out string validationErrorMessage)
    {
        result = value;
        validationErrorMessage = string.Empty;
        return true;
    }

    private async Task HandleChangedAsync(ChangeEventArgs args)
    {
        CurrentValueAsString = args.Value?.ToString();
        await OnValueChanged.InvokeAsync(CurrentValueAsString);
    }
}
