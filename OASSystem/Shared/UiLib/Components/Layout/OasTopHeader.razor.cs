using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace OAS.UiLib.Components.Layout;

public partial class OasTopHeader : IDisposable
{
    private readonly CancellationTokenSource _clockCancellation = new();
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public string SystemName { get; set; } = "OAS System";

    [Parameter]
    public string DatabaseName { get; set; } = "Default";

    [Parameter]
    public string ConnectionStatus { get; set; } = "متصل";

    [Parameter]
    public string VersionText { get; set; } = "1.0.0";

    private string UserName { get; set; } = "المستخدم";
    private string UserJobTitle { get; set; } = "مستخدم النظام";
    private string UserInitial { get; set; } = "م";
    private DateTimeOffset CurrentLocalTime { get; set; } = DateTimeOffset.Now;
    private string CurrentTimeText => CurrentLocalTime.ToString("hh:mm tt");
    private string CurrentDateText => CurrentLocalTime.ToString("yyyy/MM/dd");

    protected override async Task OnInitializedAsync()
    {
        await LoadUserAsync();
        _ = RunClockAsync(_clockCancellation.Token);
    }

    private async Task LoadUserAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
            return;

        var firstName = user.FindFirst(ClaimTypes.GivenName)?.Value ?? user.FindFirst("given_name")?.Value;
        var lastName = user.FindFirst(ClaimTypes.Surname)?.Value ?? user.FindFirst("family_name")?.Value;
        var composedName = string.Join(' ', new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var displayName = user.FindFirst("display_name")?.Value;

        UserName = !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : !string.IsNullOrWhiteSpace(composedName)
                ? composedName
                : user.FindFirst(ClaimTypes.Name)?.Value
                  ?? user.FindFirst("name")?.Value
                  ?? user.FindFirst("preferred_username")?.Value
                  ?? user.Identity.Name
                  ?? "المستخدم";

        UserJobTitle = user.IsInRole("Administrator")
            ? "مدير النظام"
            : user.FindFirst("job_title")?.Value
              ?? user.FindFirst("JobTitle")?.Value
              ?? "مستخدم النظام";

        UserInitial = GetInitial(UserName);
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static string GetInitial(string name)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "م" : trimmed[..1];
    }

    private void OpenSettings() => Navigation.NavigateTo("/settings");

    public void Dispose()
    {
        _clockCancellation.Cancel();
        _clockCancellation.Dispose();
        GC.SuppressFinalize(this);
    }
}
