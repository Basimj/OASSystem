using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace OAS.UiLib.Components.Layout;

public partial class UiAccountMenu
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public string DisplayName { get; set; } = string.Empty;
    [Parameter] public string UserName { get; set; } = string.Empty;
    [Parameter] public string? Email { get; set; }
    [Parameter] public string Initials { get; set; } = string.Empty;
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public string EditProfileText { get; set; } = "تعديل الملف الشخصي";
    [Parameter] public string SettingsText { get; set; } = "الإعدادات";
    [Parameter] public string SupportText { get; set; } = "دعم المستخدم";
    [Parameter] public string LogoutText { get; set; } = "تسجيل الخروج";
    [Parameter] public EventCallback<MouseEventArgs> OnEditProfile { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnSettings { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnSupport { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnLogout { get; set; }
    private Task EditProfileAsync(MouseEventArgs e)=>OnEditProfile.InvokeAsync(e);
    private Task SettingsAsync(MouseEventArgs e)=>OnSettings.InvokeAsync(e);
    private Task SupportAsync(MouseEventArgs e)=>OnSupport.InvokeAsync(e);
    private Task LogoutAsync(MouseEventArgs e)=>OnLogout.InvokeAsync(e);
}
