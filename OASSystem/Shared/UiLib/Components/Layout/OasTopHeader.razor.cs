using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace OAS.UiLib.Components.Layout;

public partial class OasTopHeader : IDisposable
{
    private readonly CancellationTokenSource _clockCancellation = new();
    private bool _accountMenuOpen;

    [Parameter] public string SystemName { get; set; } = "OAS System";
    [Parameter] public string? SystemLogoUrl { get; set; }
    [Parameter] public string DatabaseName { get; set; } = "Default";
    [Parameter] public string ConnectionStatus { get; set; } = "متصل";
    [Parameter] public string VersionText { get; set; } = "1.0.0";
    [Parameter] public string UserDisplayName { get; set; } = "المستخدم";
    [Parameter] public string UserName { get; set; } = string.Empty;
    [Parameter] public string? UserEmail { get; set; }
    [Parameter] public string? UserImageUrl { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnEditProfile { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnSettings { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnSupport { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnLogout { get; set; }

    private DateTimeOffset CurrentLocalTime { get; set; } = DateTimeOffset.Now;
    private string CurrentTimeText => CurrentLocalTime.ToString("hh:mm tt");
    private string CurrentDateText => CurrentLocalTime.ToString("yyyy/MM/dd");
    private string UserInitials => GetInitials(UserDisplayName);

    protected override Task OnInitializedAsync()
    {
        _ = RunClockAsync(_clockCancellation.Token);
        return Task.CompletedTask;
    }

    private async Task RunClockAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                CurrentLocalTime = DateTimeOffset.Now;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private void ToggleAccountMenu() => _accountMenuOpen = !_accountMenuOpen;
    private void CloseAccountMenu() => _accountMenuOpen = false;
    private async Task EditProfileAsync(MouseEventArgs args) { _accountMenuOpen = false; await OnEditProfile.InvokeAsync(args); }
    private async Task SettingsAsync(MouseEventArgs args) { _accountMenuOpen = false; await OnSettings.InvokeAsync(args); }
    private async Task SupportAsync(MouseEventArgs args) { _accountMenuOpen = false; await OnSupport.InvokeAsync(args); }
    private async Task LogoutAsync(MouseEventArgs args) { _accountMenuOpen = false; await OnLogout.InvokeAsync(args); }

    private static string GetInitials(string value)
    {
        var parts = (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "م";
        return string.Concat(parts.Take(2).Select(x => x[..1])).ToUpperInvariant();
    }

    public void Dispose()
    {
        _clockCancellation.Cancel();
        _clockCancellation.Dispose();
        GC.SuppressFinalize(this);
    }
}
