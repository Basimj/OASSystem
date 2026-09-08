using NUnit.Framework;
using OAS.Domain.Exceptions;
using OAS.Domain.Identity.Constants;
using OAS.Domain.Identity.Entities;

namespace OAS.Tests.Identity.Users.Domain;

[TestFixture]
public sealed class UserAccountTests
{
    [Test]
    public void Create_ValidUser_NormalizesIdentity()
    {
        var id = Guid.NewGuid();

        var user = UserAccount.Create(
            id,
            "  basim.jazim  ",
            "باسم",
            "جاسم",
            "  Basim.Jazim@Example.com  ");

        Assert.That(user.Id, Is.EqualTo(id));
        Assert.That(user.UserName, Is.EqualTo("basim.jazim"));
        Assert.That(user.NormalizedUserName, Is.EqualTo("BASIM.JAZIM"));
        Assert.That(user.Email, Is.EqualTo("Basim.Jazim@Example.com"));
        Assert.That(user.NormalizedEmail, Is.EqualTo("BASIM.JAZIM@EXAMPLE.COM"));
        Assert.That(user.DisplayName, Is.EqualTo("باسم جاسم"));
    }

    [Test]
    public void Create_EmptyId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            UserAccount.Create(Guid.Empty, "user", "First", "Last", null));
    }

    [TestCase("", "First", "Last")]
    [TestCase("user", "", "Last")]
    [TestCase("user", "First", "")]
    public void Create_RequiredIdentityFieldMissing_ThrowsDomainException(
        string userName,
        string firstName,
        string lastName)
    {
        Assert.Throws<DomainException>(() =>
            UserAccount.Create(Guid.NewGuid(), userName, firstName, lastName, null));
    }

    [Test]
    public void RequirePasswordChange_AndCompletePasswordChange_UpdateFlag()
    {
        var user = CreateUser();

        user.RequirePasswordChange();
        Assert.That(user.MustChangePassword, Is.True);

        user.CompleteRequiredPasswordChange();
        Assert.That(user.MustChangePassword, Is.False);
    }

    [Test]
    public void ResetAccessFailures_ClearsCounterAndLockout()
    {
        var user = CreateUser();
        var now = DateTimeOffset.UtcNow;

        user.RecordFailedAccess(now, maximumAttempts: 1, lockoutDuration: TimeSpan.FromMinutes(15));
        Assert.That(user.IsLockedOut(now), Is.True);

        user.ResetAccessFailures();

        Assert.That(user.AccessFailedCount, Is.Zero);
        Assert.That(user.LockoutEndUtc, Is.Null);
    }

    [Test]
    public void PasswordlessBootstrap_IsRestrictedToSeededSuperAdminState()
    {
        var user = UserAccount.Create(
            IdentityBootstrap.SuperAdminId,
            "admin@gmail.com",
            "System",
            "Administrator",
            "admin@gmail.com",
            isActive: true,
            isSuperAdmin: true,
            mustChangePassword: true);
        user.SetPasswordHash(IdentityBootstrap.NoPasswordHash);

        Assert.That(user.CanUsePasswordlessBootstrap(), Is.True);

        user.SetPasswordHash("configured-hash");
        Assert.That(user.CanUsePasswordlessBootstrap(), Is.False);
    }

    private static UserAccount CreateUser() =>
        UserAccount.Create(Guid.NewGuid(), "user", "First", "Last", null);
}
