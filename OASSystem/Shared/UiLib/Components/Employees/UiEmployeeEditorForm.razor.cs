using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.UiLib.Core.Models;

namespace OAS.UiLib.Components.Employees;

public partial class UiEmployeeEditorForm
{
    [Parameter, EditorRequired]
    public UiEmployeeEditorModel Model { get; set; } = new();

    [Parameter]
    public bool IsEditable { get; set; }

    [Parameter]
    public bool IsNew { get; set; }

    [Parameter]
    public bool UserAccountLinkLocked { get; set; }

    [Parameter]
    public string? ImageUrl { get; set; }

    [Parameter]
    public bool ShowRemoveImage { get; set; }

    [Parameter]
    public IReadOnlyList<UiSelectOption> JobTitleOptions { get; set; } = [];

    [Parameter]
    public UiLookupItem? LinkedUserItem { get; set; }

    [Parameter]
    public Func<string, CancellationToken, Task<IReadOnlyList<UiLookupItem>>>? UserSearchAsync { get; set; }

    [Parameter]
    public EventCallback OnChanged { get; set; }

    [Parameter]
    public EventCallback<IBrowserFile> OnImageSelected { get; set; }

    [Parameter]
    public EventCallback OnImageRemoved { get; set; }

    private string EmployeeCodeText =>
        string.IsNullOrWhiteSpace(Model.EmployeeCode)
            ? "—"
            : Model.EmployeeCode;

    private string? JobTitleValue =>
        Model.JobTitleId?.ToString("D");

    private string? UserAccountValue =>
        Model.UserAccountId?.ToString("D");

    private string DisplayName =>
        $"{Model.FirstName} {Model.LastName}".Trim();

    private string Initials
    {
        get
        {
            var parts = DisplayName.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
                return "م";

            if (parts.Length == 1)
                return parts[0][..1].ToUpperInvariant();

            return string.Concat(
                parts.Take(2).Select(x => x[..1]))
                .ToUpperInvariant();
        }
    }

    private async Task SetFirstNameAsync(string? value)
    {
        Model.FirstName = value ?? string.Empty;
        await OnChanged.InvokeAsync();
    }

    private async Task SetLastNameAsync(string? value)
    {
        Model.LastName = value ?? string.Empty;
        await OnChanged.InvokeAsync();
    }

    private async Task SetPhoneAsync(string? value)
    {
        Model.Phone = value ?? string.Empty;
        await OnChanged.InvokeAsync();
    }

    private async Task SetEmailAsync(string? value)
    {
        Model.Email = value ?? string.Empty;
        await OnChanged.InvokeAsync();
    }

    private async Task SetCountryAsync(string? value)
    {
        Model.Country = value ?? string.Empty;
        await OnChanged.InvokeAsync();
    }

    private async Task SetGovernorateAsync(string? value)
    {
        Model.Governorate = value ?? string.Empty;
        await OnChanged.InvokeAsync();
    }

    private async Task SetCityAsync(string? value)
    {
        Model.City = value ?? string.Empty;
        await OnChanged.InvokeAsync();
    }

    private async Task SetPostalCodeAsync(string? value)
    {
        Model.PostalCode = value ?? string.Empty;
        await OnChanged.InvokeAsync();
    }

    private async Task SetResidentialAddressAsync(string? value)
    {
        Model.ResidentialAddress = value ?? string.Empty;
        await OnChanged.InvokeAsync();
    }

    private async Task SetJobTitleAsync(string? value)
    {
        Model.JobTitleId =
            Guid.TryParse(value, out var id)
                ? id
                : null;

        await OnChanged.InvokeAsync();
    }

    private async Task SetHireDateAsync(DateOnly? value)
    {
        Model.HireDate = value;
        await OnChanged.InvokeAsync();
    }

    private async Task SetActiveAsync(bool value)
    {
        Model.IsActive = value;
        await OnChanged.InvokeAsync();
    }

    private async Task SetCommissionAsync(bool value)
    {
        Model.IsCommissionEligible = value;
        await OnChanged.InvokeAsync();
    }

    private async Task SetUserAccountAsync(string? value)
    {
        if (UserAccountLinkLocked)
            return;

        Model.UserAccountId =
            Guid.TryParse(value, out var id)
                ? id
                : null;

        await OnChanged.InvokeAsync();
    }
}