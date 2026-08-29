using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Inputs;

public partial class UiCheckbox
{
    [Parameter] public bool Value { get; set; }
    [Parameter] public EventCallback<bool> ValueChanged { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public bool Disabled { get; set; }

    private Task HandleChangedAsync(ChangeEventArgs args) =>
        ValueChanged.InvokeAsync(args.Value is bool value && value);
}
