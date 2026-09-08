namespace OAS.Contracts.Identity.Authentication;

public sealed record CompletePasswordSetupRequest(
    string NewPassword,
    string ConfirmPassword);
