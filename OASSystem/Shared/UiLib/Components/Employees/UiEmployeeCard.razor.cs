using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Employees;

public partial class UiEmployeeCard
{
    private bool _menuOpen;

    [Parameter] public string Name { get; set; } = string.Empty;
    [Parameter] public string EmployeeCode { get; set; } = string.Empty;
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public string? JobTitle { get; set; }
    [Parameter] public string? Phone { get; set; }
    [Parameter] public bool IsActive { get; set; }
    [Parameter] public bool IsCommissionEligible { get; set; }
    [Parameter] public EventCallback OnOpen { get; set; }
    [Parameter] public EventCallback OnToggleStatus { get; set; }

    private string EffectiveJobTitle => string.IsNullOrWhiteSpace(JobTitle) ? "غير محدد" : JobTitle;
    private string EffectivePhone => string.IsNullOrWhiteSpace(Phone) ? "غير محدد" : Phone;

    private string Initials
    {
        get
        {
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) return "؟";
            if (parts.Length == 1) return parts[0][..1].ToUpperInvariant();
            return string.Concat(parts.Take(2).Select(x => x[..1])).ToUpperInvariant();
        }
    }

    private void ToggleMenu() => _menuOpen = !_menuOpen;
    private async Task OpenAsync() { _menuOpen = false; await OnOpen.InvokeAsync(); }
    private async Task EditAsync() { _menuOpen = false; await OnOpen.InvokeAsync(); }
    private async Task ToggleStatusAsync() { _menuOpen = false; await OnToggleStatus.InvokeAsync(); }
}
