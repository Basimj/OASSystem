using NUnit.Framework;
using OAS.Application.Identity.Roles.Commands.CreateRole;
using OAS.Application.Identity.Roles.Commands.ImportRoles;
using OAS.Contracts.Identity.Roles;
using OAS.Domain.Identity.Entities;
using OAS.Tests.Identity.Users.Application;

namespace OAS.Tests.Identity.Roles.Application;

[TestFixture]
public sealed class RoleManagementHandlerTests
{
    [Test]
    public async Task CreateRole_CreatesCustomRole_WithImmutableTechnicalKey()
    {
        var repository = new FakeIdentityRepository();
        var handler = new CreateRoleCommandHandler(repository);

        var id = await handler.Handle(
            new CreateRoleCommand(new CreateRoleRequest("Cashier", "أمين صندوق")),
            CancellationToken.None);

        var role = (await repository.ListRolesAsync()).Single(x => x.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(role.Name, Is.EqualTo("Cashier"));
            Assert.That(role.DisplayName, Is.EqualTo("أمين صندوق"));
            Assert.That(role.IsSystem, Is.False);
        });
    }

    [Test]
    public async Task ImportRoles_UpdatesDisplayName_WithoutChangingSystemKey_AndCreatesCustomRoles()
    {
        var repository = new FakeIdentityRepository();
        var administrator = Role.Create(Guid.NewGuid(), "Administrator", "Administrator", isSystem: true);
        repository.SeedRole(administrator);
        var handler = new ImportRolesCommandHandler(repository);

        var result = await handler.Handle(
            new ImportRolesCommand(new ImportRolesRequest(
            [
                new RoleImportItem("Administrator", "مدير النظام"),
                new RoleImportItem("Optometrist", "أخصائي بصريات")
            ])),
            CancellationToken.None);

        var roles = await repository.ListRolesAsync();
        var updatedAdministrator = roles.Single(x => x.Id == administrator.Id);
        var optometrist = roles.Single(x => x.Name == "Optometrist");

        Assert.Multiple(() =>
        {
            Assert.That(result.CreatedCount, Is.EqualTo(1));
            Assert.That(result.UpdatedCount, Is.EqualTo(1));
            Assert.That(updatedAdministrator.Name, Is.EqualTo("Administrator"));
            Assert.That(updatedAdministrator.DisplayName, Is.EqualTo("مدير النظام"));
            Assert.That(updatedAdministrator.IsSystem, Is.True);
            Assert.That(optometrist.IsSystem, Is.False);
        });
    }
}
