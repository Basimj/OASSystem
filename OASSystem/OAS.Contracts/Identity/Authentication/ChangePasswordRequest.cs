namespace OAS.Contracts.Identity.Authentication;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
