namespace OAS.Application.Identity.Authorization;

public static class IdentityPermissions
{
    public const string UsersManage = "identity.users.manage"; // Legacy compatibility only.
    public const string UsersView = "identity.users.view";
    public const string UsersCreate = "identity.users.create";
    public const string UsersEdit = "identity.users.edit";
    public const string UsersDisable = "identity.users.disable";
    public const string UsersAssignRoles = "identity.users.assign_roles";
    public const string UsersResetPassword = "identity.users.reset_password";
    public const string UsersUnlock = "identity.users.unlock";
    public const string RolesManage = "identity.roles.manage";
}
