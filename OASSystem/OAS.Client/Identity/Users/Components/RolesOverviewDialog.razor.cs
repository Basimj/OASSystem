using Microsoft.AspNetCore.Components;
using OAS.Contracts.Identity.Roles;

namespace OAS.Client.Identity.Users.Components;

public partial class RolesOverviewDialog
{
    [Parameter] public IReadOnlyList<RoleDto> Roles { get; set; } = [];

    private string TranslateRole(string role) => role switch
    {
        "Administrator" => L["Role_Administrator"],
        "User" => L["Role_User"],
        _ => role
    };
}
