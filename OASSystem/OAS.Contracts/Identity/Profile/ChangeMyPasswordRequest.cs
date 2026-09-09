namespace OAS.Contracts.Identity.Profile;

public sealed record ChangeMyPasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);
