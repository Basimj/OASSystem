using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Employees;

public partial class UiEmployeeFilterMenu
{
    private bool _open;

    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public IReadOnlyList<UiSelectOption> Options { get; set; } = [];
    [Parameter] public bool Disabled { get; set; }

    private string SelectedText => Options.FirstOrDefault(x => string.Equals(x.Value, Value, StringComparison.Ordinal))?.Text
                                   ?? Options.FirstOrDefault()?.Text
                                   ?? "الفلتر";

    private void Toggle() { if (!Disabled) _open = !_open; }

    public void Dismiss() => _open = false;

    private async Task SelectAsync(string? value)
    {
        if (Disabled) return;
        _open = false;
        await ValueChanged.InvokeAsync(value);
    }
}
