namespace OAS.Application.Identity.Users.Models;

public enum CreateUserFailureKind
{
    None = 0,
    Validation = 1,
    Conflict = 2,
    Forbidden = 3
}
