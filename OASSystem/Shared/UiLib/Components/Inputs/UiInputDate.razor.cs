using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Inputs;

public partial class UiInputDate
{
    private readonly string _generatedInputId =
        $"oas-date-{Guid.NewGuid():N}";

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool EnableNativeValidation { get; set; } = true;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public ControlSize Size { get; set; } =
        ControlSize.Medium;

    public override Task SetParametersAsync(
        ParameterView parameters)
    {
        if (!parameters.TryGetValue<Expression<Func<DateOnly?>>>(
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

    private string SizeCss =>
        Size.ToString().ToLowerInvariant();

    private void HandleChange(ChangeEventArgs args)
    {
        CurrentValueAsString =
            args.Value?.ToString();
    }

    protected override string? FormatValueAsString(
        DateOnly? value)
    {
        return value?.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);
    }

    protected override bool TryParseValueFromString(
        string? value,
        out DateOnly? result,
        out string validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            validationErrorMessage = string.Empty;
            return true;
        }

        if (DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            result = parsed;
            validationErrorMessage = string.Empty;
            return true;
        }

        result = null;
        validationErrorMessage =
            "قيمة التاريخ غير صحيحة.";

        return false;
    }
}