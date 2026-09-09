using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Identity.Constants;

namespace OAS.Domain.Identity.Entities;

public sealed class UserAccount : AuditableEntity<Guid>
{
    private UserAccount() { }

    private UserAccount(Guid id, string userName, string firstName, string lastName, string? email, bool isActive, bool isSuperAdmin, bool mustChangePassword)
    {
        Id = id;
        SetIdentity(userName, firstName, lastName, email);
        IsActive = isActive;
        IsSuperAdmin = isSuperAdmin;
        MustChangePassword = mustChangePassword;
    }

    public string UserName { get; private set; } = string.Empty;
    public string NormalizedUserName { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsSuperAdmin { get; private set; }
    public bool MustChangePassword { get; private set; }
    public int AccessFailedCount { get; private set; }
    public DateTimeOffset? LockoutEndUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public string DisplayName => string.Join(' ', new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public static UserAccount Create(Guid id, string userName, string firstName, string lastName, string? email, bool isActive = true, bool isSuperAdmin = false, bool mustChangePassword = false)
    {
        if (id == Guid.Empty) throw new DomainException("User id is required.");
        return new UserAccount(id, userName, firstName, lastName, email, isActive, isSuperAdmin, mustChangePassword);
    }

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    public void SetIdentity(string userName, string firstName, string lastName, string? email)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new DomainException("User name is required.");
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Last name is required.");

        UserName = userName.Trim();
        NormalizedUserName = Normalize(userName);
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        NormalizedEmail = Email is null ? null : Normalize(Email);
    }

    public void UpdatePersonalName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("Last name is required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        PasswordHash = passwordHash;
    }

    public void RequirePasswordChange() => MustChangePassword = true;

    public void CompleteRequiredPasswordChange() => MustChangePassword = false;

    public bool CanUsePasswordlessBootstrap() =>
        Id == IdentityBootstrap.SuperAdminId &&
        IsSuperAdmin &&
        IsActive &&
        MustChangePassword &&
        LastLoginAtUtc is null &&
        string.Equals(PasswordHash, IdentityBootstrap.NoPasswordHash, StringComparison.Ordinal);

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        if (isActive) LockoutEndUtc = null;
    }

    public bool IsLockedOut(DateTimeOffset nowUtc) => LockoutEndUtc is not null && LockoutEndUtc > nowUtc;

    public void RecordFailedAccess(DateTimeOffset nowUtc, int maximumAttempts, TimeSpan lockoutDuration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumAttempts, 1);
        AccessFailedCount++;
        if (AccessFailedCount >= maximumAttempts)
        {
            LockoutEndUtc = nowUtc.Add(lockoutDuration);
            AccessFailedCount = 0;
        }
    }

    public void ResetAccessFailures()
    {
        AccessFailedCount = 0;
        LockoutEndUtc = null;
    }

    public void RecordSuccessfulLogin(DateTimeOffset nowUtc)
    {
        ResetAccessFailures();
        LastLoginAtUtc = nowUtc;
    }
}
