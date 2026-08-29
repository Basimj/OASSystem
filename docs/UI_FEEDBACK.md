# UI Feedback Foundation

`OAS.UiLib` owns all Snackbar/Dialog markup and CSS. Client features only call services; no feature-specific Snackbar or Dialog components are required.

## Snackbar

`UiSnackbarHost` is mounted once in `OAS.Client/Routes.razor`. Inject `IUiSnackbarService` and call `Success`, `Error`, `Warning`, or `Info`.

The client-level `IUiFeedbackSettingsService` is the settings boundary used by a future settings page. It can change Snackbar position and lifetime at runtime without modifying the UiLib component:

```csharp
feedbackSettings.Apply(new UiFeedbackSettings
{
    SnackbarPosition = UiSnackbarPosition.BottomCenter,
    SnackbarDurationMilliseconds = 5000,
    SnackbarMaxVisible = 4
});
```

Supported positions are all nine screen anchors: `TopLeft`, `TopCenter`, `TopRight`, `CenterLeft`, `Center`, `CenterRight`, `BottomLeft`, `BottomCenter`, and `BottomRight`. Duration is clamped to 1-60 seconds and visible messages to 1-10.

## API feedback

`IApiFeedbackService` converts stable API/validation error codes to localized text and sends them to the Snackbar.

Current Identity input flows use explicit expected-failure results for login, change-password, and create-user. Validation failures, incorrect credentials/current password, password-history conflicts, duplicate user/email conflicts, and expected authorization failures no longer use exceptions as their normal control flow. Infrastructure failures and other unexpected faults remain exceptions and continue through the API exception middleware.

## Password visibility

`UiInputText` supports `RevealPassword="true"`. The eye button and its accessibility labels live entirely inside `OAS.UiLib` and are reusable by login, password-change, and user forms.

## Dialog

`UiDialogHost` is mounted once in `OAS.Client/Routes.razor`.

- `IUiDialogService.ConfirmAsync(...)` opens a reusable confirmation dialog.
- `IUiDialogService.ShowAsync<TComponent>(...)` opens arbitrary Razor components through `DynamicComponent`, so forms can be hosted inside a dialog without introducing business-specific components into UiLib.

The visual components are `UiDialog`, `UiConfirmDialog`, and `UiDialogHost`. `UiDialogOptions` controls size, backdrop closing, Escape closing, and the close button.
