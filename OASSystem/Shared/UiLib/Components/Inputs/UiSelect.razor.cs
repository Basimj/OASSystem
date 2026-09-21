using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Inputs;

public partial class UiSelect
{
    private readonly string _generatedInputId =
        $"oas-select-{Guid.NewGuid():N}";

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public IReadOnlyList<UiSelectOption> Options { get; set; } = [];

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool EnableNativeValidation { get; set; } = true;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public ControlSize Size { get; set; } =
        ControlSize.Medium;

    [Parameter]
    public EventCallback<string?> OnValueChanged { get; set; }

    public override Task SetParametersAsync(
        ParameterView parameters)
    {
        if (!parameters.TryGetValue<Expression<Func<string?>>>(
                nameof(ValueExpression),
                out var valueExpression) ||
            valueExpression is null)
        {
            ValueExpression = () => Value;
        }

        return base.SetParametersAsync(parameters);
    }

    private string InputId =>
        string.IsNullOrWhiteSpace(Id)
            ? _generatedInputId
            : Id;

    private string InputCssClass =>
        $"ui-select__control " +
        $"ui-select__control--{Size.ToString().ToLowerInvariant()} " +
        $"{CssClass}"
        .Trim();

    protected override bool TryParseValueFromString(
        string? value,
        out string? result,
        out string validationErrorMessage)
    {
        result = value;
        validationErrorMessage = string.Empty;

        return true;
    }

    private async Task HandleChangedAsync(
        ChangeEventArgs args)
    {
        CurrentValueAsString =
            args.Value?.ToString();

        await OnValueChanged.InvokeAsync(
            CurrentValueAsString);
    }
}