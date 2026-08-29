namespace OAS.Application.Identity.Authentication.Models;

public enum ChangePasswordFailureKind
{
    None = 0,
    Validation = 1,
    CurrentPasswordInvalid = 2,
    PasswordReused = 3,
    Forbidden = 4
}
