using NUnit.Framework;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Authentication.Commands;
using OAS.Application.Identity.Users.Commands.CreateUser;
using OAS.Application.Identity.Users.Commands.ResetUserPassword;
using OAS.Application.Identity.Users.Commands.SetUserRoles;
using OAS.Application.Identity.Users.Commands.SetUserStatus;
using OAS.Application.Identity.Users.Queries.GetUserById;
using OAS.Application.Identity.Users.Queries.GetUsers;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Identity.Authentication;
using OAS.Contracts.Identity.Users;
using OAS.Domain.Identity.Entities;

namespace OAS.Tests.Identity.Users.Application;

[TestFixture]
public sealed class UserManagementHandlerTests
{
    [Test]
    public async Task CreateUser_GeneratesTemporaryPassword_AndRequiresPasswordSetup()
    {
        var repository = new FakeIdentityRepository();
        var userRole = Role.Create(Guid.NewGuid(), "User", "User");
        repository.SeedRole(userRole);
        var passwordService = new FakePasswordService();
        var handler = new CreateUserCommandHandler(
            repository,
            passwordService,
            new FixedTemporaryPasswordGenerator("Temp!Password42"));

        var outcome = await handler.Handle(
            new CreateUserCommand(new CreateUserRequest(
                "new.user",
                "New",
                "User",
                "new.user@example.com",
                "+967771234567",
                [userRole.Id],
                true)),
            CancellationToken.None);

        var created = repository.Users.Single(x => x.Id == outcome.UserId);
        Assert.Multiple(() =>
        {
            Assert.That(outcome.TemporaryPassword, Is.EqualTo("Temp!Password42"));
            Assert.That(created.MustChangePassword, Is.True);
            Assert.That(created.PasswordHash, Is.EqualTo("hash::Temp!Password42"));
            Assert.That(repository.PasswordHistory, Is.Empty, "Temporary credentials are not permanent-password history.");
        });
    }

    [Test]
    public async Task ResetPassword_ReplacesCredential_RequiresSetup_AndClearsLockout()
    {
        var repository = new FakeIdentityRepository();
        var target = UserAccount.Create(Guid.NewGuid(), "locked.user", "Locked", "User", null);
        target.SetPasswordHash("hash::Old!Password42");
        target.RecordFailedAccess(DateTimeOffset.UtcNow, 1, TimeSpan.FromMinutes(15));
        repository.SeedUser(target);

        var handler = new ResetUserPasswordCommandHandler(
            repository,
            new FakePasswordService(),
            new FixedTemporaryPasswordGenerator("Reset!Password42"),
            new FakeCurrentUser(Guid.NewGuid()),
            TimeProvider.System);

        var temporaryPassword = await handler.Handle(
            new ResetUserPasswordCommand(target.Id, new ResetUserPasswordRequest(Convert.ToBase64String(target.RowVersion))),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(temporaryPassword, Is.EqualTo("Reset!Password42"));
            Assert.That(target.PasswordHash, Is.EqualTo("hash::Reset!Password42"));
            Assert.That(target.MustChangePassword, Is.True);
            Assert.That(target.AccessFailedCount, Is.Zero);
            Assert.That(target.LockoutEndUtc, Is.Null);
        });
    }

    [Test]
    public async Task ResetPassword_PreservesCurrentPermanentPasswordInHistory()
    {
        var repository = new FakeIdentityRepository();
        var target = UserAccount.Create(Guid.NewGuid(), "history.user", "History", "User", null);
        target.SetPasswordHash("hash::Permanent!Password42");
        repository.SeedUser(target);

        var handler = new ResetUserPasswordCommandHandler(
            repository,
            new FakePasswordService(),
            new FixedTemporaryPasswordGenerator("Reset!Password42"),
            new FakeCurrentUser(Guid.NewGuid()),
            TimeProvider.System);

        await handler.Handle(
            new ResetUserPasswordCommand(target.Id, new ResetUserPasswordRequest(Convert.ToBase64String(target.RowVersion))),
            CancellationToken.None);

        Assert.That(repository.PasswordHistory.Single().PasswordHash, Is.EqualTo("hash::Permanent!Password42"));
    }

    [Test]
    public void CompletePasswordSetup_CannotReuseTemporaryPassword()
    {
        var repository = new FakeIdentityRepository();
        var user = UserAccount.Create(Guid.NewGuid(), "setup.user", "Setup", "User", null, mustChangePassword: true);
        user.SetPasswordHash("hash::Temp!Password42");
        repository.SeedUser(user);

        var handler = new CompletePasswordSetupCommandHandler(
            new FakeCurrentUser(user.Id),
            repository,
            new FakePasswordService(),
            TimeProvider.System);

        var exception = Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new CompletePasswordSetupCommand(new CompletePasswordSetupRequest("Temp!Password42", "Temp!Password42")),
            CancellationToken.None));

        Assert.That(exception!.Code, Is.EqualTo("identity_password_reused"));
    }

    [Test]
    public async Task CompletePasswordSetup_SavesPermanentPasswordToHistory_AndClearsRequirement()
    {
        var repository = new FakeIdentityRepository();
        var user = UserAccount.Create(Guid.NewGuid(), "setup.user", "Setup", "User", null, mustChangePassword: true);
        user.SetPasswordHash("hash::Temp!Password42");
        repository.SeedUser(user);

        var handler = new CompletePasswordSetupCommandHandler(
            new FakeCurrentUser(user.Id),
            repository,
            new FakePasswordService(),
            TimeProvider.System);

        var result = await handler.Handle(
            new CompletePasswordSetupCommand(new CompletePasswordSetupRequest("Permanent!Password42", "Permanent!Password42")),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.MustChangePassword, Is.False);
            Assert.That(user.MustChangePassword, Is.False);
            Assert.That(user.PasswordHash, Is.EqualTo("hash::Permanent!Password42"));
            Assert.That(repository.PasswordHistory.Single().PasswordHash, Is.EqualTo("hash::Permanent!Password42"));
        });
    }

    [Test]
    public void DisableCurrentUser_IsRejected()
    {
        var repository = new FakeIdentityRepository();
        var user = UserAccount.Create(Guid.NewGuid(), "admin", "Admin", "User", null);
        user.SetPasswordHash("hash::Password!42");
        repository.SeedUser(user);
        var handler = new SetUserStatusCommandHandler(repository, new FakeCurrentUser(user.Id));

        var exception = Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new SetUserStatusCommand(user.Id, new SetUserStatusRequest(false, Convert.ToBase64String(user.RowVersion))),
            CancellationToken.None));

        Assert.That(exception!.Code, Is.EqualTo("identity_cannot_disable_current_user"));
    }

    [Test]
    public async Task Administrator_CanAssignAdministratorRole_ToRegularUser()
    {
        var repository = new FakeIdentityRepository();
        var administratorRole = Role.Create(Guid.NewGuid(), "Administrator", "Administrator", true);
        var userRole = Role.Create(Guid.NewGuid(), "User", "User", true);
        repository.SeedRole(administratorRole);
        repository.SeedRole(userRole);

        var actor = UserAccount.Create(Guid.NewGuid(), "manager", "Manager", "User", null);
        actor.SetPasswordHash("hash::Password!42");
        repository.SeedUser(actor, administratorRole.Id);

        var target = UserAccount.Create(Guid.NewGuid(), "target", "Target", "User", null);
        target.SetPasswordHash("hash::Password!42");
        repository.SeedUser(target, userRole.Id);

        var handler = new SetUserRolesCommandHandler(repository, new FakeCurrentUser(actor.Id));
        await handler.Handle(
            new SetUserRolesCommand(target.Id, new SetUserRolesRequest([administratorRole.Id], Convert.ToBase64String(target.RowVersion))),
            CancellationToken.None);

        var updated = await repository.GetUserAsync(target.Id, false);
        Assert.That(updated!.Roles.Select(x => x.Name), Does.Contain("Administrator"));
    }

    [Test]
    public async Task GetUsers_HidesSuperAdmin_FromRegularAdministrator()
    {
        var repository = new FakeIdentityRepository();
        var administratorRole = Role.Create(Guid.NewGuid(), "Administrator", "Administrator", true);
        repository.SeedRole(administratorRole);

        var actor = UserAccount.Create(Guid.NewGuid(), "manager", "Manager", "User", null);
        actor.SetPasswordHash("hash::Password!42");
        repository.SeedUser(actor, administratorRole.Id);

        var superAdmin = UserAccount.Create(Guid.NewGuid(), "root", "Super", "Admin", null, isSuperAdmin: true);
        superAdmin.SetPasswordHash("hash::Password!42");
        repository.SeedUser(superAdmin, administratorRole.Id);

        var regular = UserAccount.Create(Guid.NewGuid(), "regular", "Regular", "User", null);
        regular.SetPasswordHash("hash::Password!42");
        repository.SeedUser(regular, administratorRole.Id);

        var handler = new GetUsersQueryHandler(repository, new FakeCurrentUser(actor.Id), TimeProvider.System);
        var result = await handler.Handle(new GetUsersQuery(new PageRequest { PageNumber = 1, PageSize = 20 }), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items.Any(x => x.Id == superAdmin.Id), Is.False);
            Assert.That(result.Items.Any(x => x.Id == regular.Id), Is.True);
            Assert.That(result.TotalCount, Is.EqualTo(2)); // actor + regular user
        });
    }

    [Test]
    public async Task GetUserById_ResolvesAuditUserIds_ToUserNames()
    {
        var repository = new FakeIdentityRepository();
        var actor = UserAccount.Create(Guid.NewGuid(), "audit.admin", "Audit", "Admin", null);
        actor.SetPasswordHash("hash::Password!42");
        repository.SeedUser(actor);

        var target = UserAccount.Create(Guid.NewGuid(), "audit.target", "Audit", "Target", null);
        target.SetPasswordHash("hash::Password!42");
        target.SetCreatedAudit(DateTimeOffset.UtcNow.AddDays(-1), actor.Id.ToString());
        target.SetModifiedAudit(DateTimeOffset.UtcNow, actor.Id.ToString());
        repository.SeedUser(target);

        var handler = new GetUserByIdQueryHandler(repository, new FakeCurrentUser(actor.Id), TimeProvider.System);
        var details = await handler.Handle(new GetUserByIdQuery(target.Id), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(details.CreatedBy, Is.EqualTo("audit.admin"));
            Assert.That(details.LastModifiedBy, Is.EqualTo("audit.admin"));
        });
    }

    [Test]
    public async Task GetUserById_DoesNotExposeSuperAdminUserName_ThroughAuditMetadata()
    {
        var repository = new FakeIdentityRepository();
        var actor = UserAccount.Create(Guid.NewGuid(), "manager", "Manager", "User", null);
        actor.SetPasswordHash("hash::Password!42");
        repository.SeedUser(actor);

        var superAdmin = UserAccount.Create(Guid.NewGuid(), "root.hidden", "Root", "Hidden", null, isSuperAdmin: true);
        superAdmin.SetPasswordHash("hash::Password!42");
        repository.SeedUser(superAdmin);

        var target = UserAccount.Create(Guid.NewGuid(), "created.by.root", "Regular", "User", null);
        target.SetPasswordHash("hash::Password!42");
        target.SetCreatedAudit(DateTimeOffset.UtcNow, superAdmin.Id.ToString());
        repository.SeedUser(target);

        var handler = new GetUserByIdQueryHandler(repository, new FakeCurrentUser(actor.Id), TimeProvider.System);
        var details = await handler.Handle(new GetUserByIdQuery(target.Id), CancellationToken.None);

        Assert.That(details.CreatedBy, Is.Null);
    }

}
