using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Inputs;

public partial class UiSearchableSelect
{
    private readonly string _triggerId = $"oas-searchable-select-{Guid.NewGuid():N}";
    private readonly string _popoverId = $"oas-searchable-options-{Guid.NewGuid():N}";
    private readonly string _errorId = $"oas-searchable-error-{Guid.NewGuid():N}";
    private string _search = string.Empty;

    [Parameter] public string? Label { get; set; }
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public Expression<Func<string?>>? ValueExpression { get; set; }
    [Parameter] public IReadOnlyList<UiSelectOption> Options { get; set; } = [];
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string? ErrorText { get; set; }
    [Parameter] public string EmptyText { get; set; } = "لا توجد عناصر مطابقة.";
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public ControlSize Size { get; set; } = ControlSize.Small;

    // Avoid showing a second empty action for filters that already include "كل ...".
    private bool ShowClearOption => !Required && !Options.Any(x => string.IsNullOrEmpty(x.Value));
    private bool HasError => !string.IsNullOrWhiteSpace(ErrorText);
    private string DialogTitle => $"اختر {Label ?? "عنصرًا"}";
    private string TriggerClass => $"ui-searchable-select__trigger ui-searchable-select__trigger--{Size.ToString().ToLowerInvariant()}" +
                                   (HasError ? " ui-searchable-select__trigger--invalid" : string.Empty);
    // Native selects treat null and an empty string as the same unselected value.
    // Preserve that behavior for filters such as "كل المخازن".
    private bool IsSelected(string? optionValue) =>
        string.Equals(optionValue ?? string.Empty, Value ?? string.Empty, StringComparison.Ordinal);
    private string SelectedText => Options.FirstOrDefault(x => IsSelected(x.Value))?.Text ?? Placeholder ?? "اختر...";

    private IReadOnlyList<UiSelectOption> FilteredOptions =>
        string.IsNullOrWhiteSpace(_search)
            ? Options
            : Options.Where(x => x.Text.Contains(_search.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();

    private void ResetSearch() => _search = string.Empty;
    private void OnSearchInput(ChangeEventArgs e) => _search = e.Value?.ToString() ?? string.Empty;
    private Task SelectAsync(string? value) => ValueChanged.InvokeAsync(value);
}
