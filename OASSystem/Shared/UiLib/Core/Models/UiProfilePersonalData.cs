namespace OAS.UiLib.Core.Models;

public sealed record UiProfilePersonalData(string FirstName, string LastName);
public sealed record UiPasswordChangeData(string CurrentPassword, string NewPassword, string ConfirmPassword);
