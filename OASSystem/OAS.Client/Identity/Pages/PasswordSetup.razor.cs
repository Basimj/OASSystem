using Microsoft.AspNetCore.Components;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Identity.Services;
using OAS.Client.Identity.State;
using OAS.Contracts.Identity.Authentication;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Identity.Pages;

public partial class PasswordSetup
{
    [Inject] private IAuthClientService AuthService { get; set; } = default!;
    [Inject] private OasAuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    private readonly PasswordSetupModel _model = new();
    private bool _saving;

    protected override async Task OnInitializedAsync()
    {
        await AuthState.GetAuthenticationStateAsync();
        if (AuthState.CurrentUser?.MustChangePassword != true)
            Navigation.NavigateTo("/", replace: true);
    }

    private async Task SubmitAsync()
    {
        if (_saving) return;
        _saving = true;
        try
        {
            var result = await AuthService.CompletePasswordSetupAsync(
                new CompletePasswordSetupRequest(_model.NewPassword, _model.ConfirmPassword));
            if (!result.Succeeded || result.Value is null)
            {
                if (result.Error is not null) ApiFeedback.Show(result.Error); else ApiFeedback.ShowUnexpected();
                return;
            }

            AuthState.SetAuthenticated(result.Value);
            Snackbar.Success(L["PasswordSetup_Success"]);
            Navigation.NavigateTo("/", replace: true);
        }
        finally
        {
            _saving = false;
        }
    }

    private sealed class PasswordSetupModel
    {
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
