using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.UiLib.Components.Dialogs;

public partial class UiProfileEditorDialog
{
    private string _section = "profile";
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _userName = string.Empty;
    private string? _email;
    private string _phoneNumber = string.Empty;
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string? _imageUrl;
    private bool _savingProfile;
    private bool _changingPassword;

    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    [Parameter] public string FirstName { get; set; } = string.Empty;
    [Parameter] public string LastName { get; set; } = string.Empty;
    [Parameter] public string UserName { get; set; } = string.Empty;
    [Parameter] public string? Email { get; set; }
    [Parameter] public string? PhoneNumber { get; set; }
    [Parameter] public string? ImageUrl { get; set; }
    [Parameter] public string ProfileText { get; set; } = "البيانات الشخصية";
    [Parameter] public string PasswordText { get; set; } = "كلمة المرور";
    [Parameter] public string FirstNameText { get; set; } = "الاسم الأول";
    [Parameter] public string LastNameText { get; set; } = "الاسم الأخير";
    [Parameter] public string UserNameText { get; set; } = "اسم المستخدم";
    [Parameter] public string EmailText { get; set; } = "البريد الإلكتروني";
    [Parameter] public string PhoneNumberText { get; set; } = "رقم الجوال";
    [Parameter] public string ChangePhotoText { get; set; } = "تغيير الصورة";
    [Parameter] public string RemovePhotoText { get; set; } = "حذف الصورة";
    [Parameter] public string CurrentPasswordText { get; set; } = "كلمة المرور الحالية";
    [Parameter] public string NewPasswordText { get; set; } = "كلمة المرور الجديدة";
    [Parameter] public string ConfirmPasswordText { get; set; } = "تأكيد كلمة المرور";
    [Parameter] public string PasswordPolicyText { get; set; } = "8 أحرف على الأقل وتحتوي على حرف كبير وصغير ورقم ورمز.";
    [Parameter] public string SaveText { get; set; } = "حفظ";
    [Parameter] public string ChangePasswordText { get; set; } = "تغيير كلمة المرور";
    [Parameter] public Func<UiProfilePersonalData, Task<UiOperationFeedback>>? OnSaveProfile { get; set; }
    [Parameter] public Func<UiPasswordChangeData, Task<UiOperationFeedback>>? OnChangePassword { get; set; }
    [Parameter] public Func<IBrowserFile, Task<UiOperationFeedback>>? OnUploadImage { get; set; }
    [Parameter] public Func<Task<UiOperationFeedback>>? OnRemoveImage { get; set; }

    private string DisplayName => string.Join(' ', new[] { _firstName, _lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    private string Initials => string.Concat(new[] { _firstName, _lastName }.Where(x => !string.IsNullOrWhiteSpace(x)).Take(2).Select(x => x.Trim()[..1])).ToUpperInvariant();
    private IReadOnlyList<UiSectionTabItem> Tabs =>
    [
        new("profile", ProfileText, _section == "profile"),
        new("password", PasswordText, _section == "password")
    ];

    protected override void OnInitialized()
    {
        _firstName = FirstName;
        _lastName = LastName;
        _userName = UserName;
        _email = Email;
        _phoneNumber = PhoneNumber ?? string.Empty;
        _imageUrl = ImageUrl;
    }

    private Task SelectSectionAsync(string key) { _section = key; return Task.CompletedTask; }

    private async Task SaveProfileAsync(MouseEventArgs _)
    {
        if (OnSaveProfile is null || _savingProfile) return;
        _savingProfile = true;
        try
        {
            var result = await OnSaveProfile(new UiProfilePersonalData(_firstName, _lastName, _phoneNumber));
            ShowFeedback(result);
        }
        finally { _savingProfile = false; }
    }

    private async Task ChangePasswordAsync(MouseEventArgs _)
    {
        if (OnChangePassword is null || _changingPassword) return;
        _changingPassword = true;
        try
        {
            var result = await OnChangePassword(new UiPasswordChangeData(_currentPassword, _newPassword, _confirmPassword));
            ShowFeedback(result);
            if (result.Succeeded) _currentPassword = _newPassword = _confirmPassword = string.Empty;
        }
        finally { _changingPassword = false; }
    }

    private async Task ImageSelectedAsync(IBrowserFile file)
    {
        if (OnUploadImage is null) return;
        var result = await OnUploadImage(file);
        ShowFeedback(result);
        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.Value)) _imageUrl = result.Value;
    }

    private async Task RemoveImageAsync()
    {
        if (OnRemoveImage is null) return;
        var result = await OnRemoveImage();
        ShowFeedback(result);
        if (result.Succeeded) _imageUrl = null;
    }

    private void ShowFeedback(UiOperationFeedback result)
    {
        if (string.IsNullOrWhiteSpace(result.Message)) return;
        if (result.Succeeded) Snackbar.Success(result.Message);
        else Snackbar.Error(result.Message);
    }
}
