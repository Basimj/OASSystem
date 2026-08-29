namespace OAS.Application.Identity.Abstractions;

public enum PasswordCheckResult
{
    Failed = 0,
    Success = 1,
    SuccessRehashNeeded = 2
}
