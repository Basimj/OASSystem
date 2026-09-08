using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace OAS.UiLib.Components.Layout;

public partial class OasTopHeader : IDisposable
{
  
    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;


    private string UserName { get; set; } = "المستخدم";

    private string UserJobTitle { get; set; } = "مستخدم النظام";

    private string UserInitial { get; set; } = "م";

    private string CurrentTime { get; set; } = string.Empty;

    private string HijriDate { get; set; } = string.Empty;

    private Timer? _timer;


    protected override async Task OnInitializedAsync()
    {
        await LoadUserAsync();

        UpdateDateTime();

        _timer = new Timer(
            _ =>
            {
                _ = InvokeAsync(() =>
                {
                    UpdateDateTime();
                    StateHasChanged();
                });
            },
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1));
    }


    private async Task LoadUserAsync()
    {
        var authState =
            await AuthenticationStateProvider.GetAuthenticationStateAsync();

        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            UserName = "المستخدم";
            UserJobTitle = "مستخدم النظام";
            UserInitial = "م";
            return;
        }


        // =====================================================
        // User Name
        // =====================================================

        UserName =
            user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("name")?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.Identity.Name
            ?? "المستخدم";


        // =====================================================
        // Job Title
        // =====================================================

        if (user.IsInRole("Administrator"))
        {
            UserJobTitle = "مدير النظام";
        }
        else
        {
            UserJobTitle =
                user.FindFirst("job_title")?.Value
                ?? user.FindFirst("JobTitle")?.Value
                ?? "مستخدم النظام";
        }


        // =====================================================
        // Initial
        // =====================================================

        UserInitial = GetInitial(UserName);
    }


    private void UpdateDateTime()
    {
        var now = DateTime.Now;

        CurrentTime = now.ToString(
            "hh:mm tt",
            CultureInfo.GetCultureInfo("ar-SA"));


        var calendar = new UmAlQuraCalendar();

        var year = calendar.GetYear(now);
        var month = calendar.GetMonth(now);
        var day = calendar.GetDayOfMonth(now);

        HijriDate =
            $"{year:0000}/{month:00}/{day:00}";
    }


    private static string GetInitial(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "م";
        }

        var trimmed = name.Trim();

        return trimmed[..1];
    }





    private void OpenSettings()
    {
        Navigation.NavigateTo("/settings");
    }


    public void Dispose()
    {
        _timer?.Dispose();
    }
}