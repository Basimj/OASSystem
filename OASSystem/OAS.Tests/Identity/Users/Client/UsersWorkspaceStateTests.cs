using NUnit.Framework;
using OAS.Client.Identity.Users.Workspace;
using OAS.Contracts.Identity.Users;

namespace OAS.Tests.Identity.Users.Client;

[TestFixture]
public sealed class UsersWorkspaceStateTests
{
    [Test]
    public void DefaultState_HasNoInternalUserTabs_AndKeepsEditorEmpty()
    {
        var state = new UsersWorkspaceState();

        Assert.Multiple(() =>
        {
            Assert.That(state.SelectedUserId, Is.Null);
            Assert.That(state.Mode, Is.EqualTo(UserEditorMode.Empty));
            Assert.That(state.IsDirty, Is.False);
        });
    }

    [Test]
    public void BeginCreate_UsesSingleEditorState()
    {
        var state = new UsersWorkspaceState();
        state.BeginCreate();
        state.Editor.Form.FirstName = "Ahmed";

        Assert.Multiple(() =>
        {
            Assert.That(state.Mode, Is.EqualTo(UserEditorMode.Create));
            Assert.That(state.SelectedUserId, Is.Null);
            Assert.That(state.Editor.Form.FirstName, Is.EqualTo("Ahmed"));
            Assert.That(state.IsDirty, Is.True);
        });
    }

    [Test]
    public void LoadDetails_OpensExistingUserInReadOnlyViewMode()
    {
        var state = new UsersWorkspaceState();
        var id = Guid.NewGuid();
        var details = CreateDetails(id);

        state.SelectExisting(id, "System Administrator");
        state.LoadDetails(details);

        Assert.Multiple(() =>
        {
            Assert.That(state.SelectedUserId, Is.EqualTo(id));
            Assert.That(state.Mode, Is.EqualTo(UserEditorMode.View));
            Assert.That(state.Editor.Form.UserName, Is.EqualTo("admin"));
            Assert.That(state.IsDirty, Is.False);
        });
    }

    [Test]
    public void BeginEdit_KeepsLoadedValues_AndAllowsDirtyState()
    {
        var state = new UsersWorkspaceState();
        var id = Guid.NewGuid();
        state.LoadDetails(CreateDetails(id));

        state.BeginEdit();
        state.Editor.Form.FirstName = "Updated";

        Assert.Multiple(() =>
        {
            Assert.That(state.Mode, Is.EqualTo(UserEditorMode.Edit));
            Assert.That(state.Editor.Form.LastName, Is.EqualTo("Administrator"));
            Assert.That(state.IsDirty, Is.True);
        });
    }

    private static UserDetailsDto CreateDetails(Guid id) => new(
        id,
        "admin",
      
        "System",
        "Administrator",
        "System Administrator",
        "admin@gmail.com",
          "EMP-00501",
        true,
        true,
        false,
        0,
        null,
        false,
        DateTimeOffset.UtcNow,
        [],
        ["Administrator"],
        DateTimeOffset.UtcNow,
        null,
        null,
        null,
        Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }));
}
