
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Inputs;

public partial class UiInputText
{
    [Parameter] public string? Id { get; set; }

    [Parameter] public string Type { get; set; } = "text";

    [Parameter] public string? Label { get; set; }

    [Parameter] public string? LabelResourceKey { get; set; }

    [Parameter] public string? Placeholder { get; set; }

    [Parameter] public string? PlaceholderResourceKey { get; set; }

    [Parameter] public bool Required { get; set; }

    [Parameter] public bool EnableNativeValidation { get; set; } = true;

    [Parameter] public bool ReadOnly { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public int? MaxLength { get; set; }

    [Parameter] public string? AutoComplete { get; set; }

    [Parameter] public string? InputMode { get; set; }

    [Parameter]
    public bool ShowValidationMessage { get; set; } = true;

    [Parameter] public bool RevealPassword { get; set; }

    [Parameter]
    public ControlSize Size { get; set; } = ControlSize.Medium;

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    private bool _passwordRevealed;

    private bool CanRevealPassword =>
        RevealPassword &&
        string.Equals(
            Type,
            "password",
            StringComparison.OrdinalIgnoreCase);

    private string EffectiveType =>
        CanRevealPassword && _passwordRevealed
            ? "text"
            : Type;

    private string RevealButtonLabel =>
        _passwordRevealed
            ? L["HidePassword"].Value
            : L["ShowPassword"].Value;

    private string InputId =>
        string.IsNullOrWhiteSpace(Id)
            ? $"oas-input-{FieldIdentifier.FieldName}"
            : Id;

    private string ResolvedPlaceholder =>
        !string.IsNullOrWhiteSpace(PlaceholderResourceKey)
            ? L[PlaceholderResourceKey].Value
            : Placeholder ?? string.Empty;

    private string InputCssClass =>
        $"ui-input-text__control " +
        $"ui-input-text__control--{Size.ToString().ToLowerInvariant()} " +
        $"{(CanRevealPassword ? "ui-input-text__control--reveal" : null)} " +
        $"{CssClass}"
        .Trim();

    private IReadOnlyDictionary<string, object>?
        SafeAdditionalAttributes =>
        AdditionalAttributes?
            .Where(x =>
                !BlockedAttributeNames.Contains(x.Key))
            .ToDictionary(
                x => x.Key,
                x => x.Value);

    private static readonly HashSet<string>
        BlockedAttributeNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "id",
                "type",
                "class",
                "value",
                "placeholder",
                "readonly",
                "disabled",
                "required",
                "aria-required",
                "maxlength",
                "autocomplete",
                "inputmode"
            };

    protected override bool TryParseValueFromString(
        string? value,
        out string? result,
        out string validationErrorMessage)
    {
        result = value;
        validationErrorMessage = string.Empty;

        return true;
    }

    private void TogglePasswordVisibility() =>
        _passwordRevealed = !_passwordRevealed;
}

