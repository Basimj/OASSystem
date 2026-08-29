using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace OAS.UiLib.Components.Navigation;

public partial class UiNavLink
{
    [Parameter, EditorRequired] public string Href { get; set; } = string.Empty;
    [Parameter] public string? Text { get; set; }
    [Parameter] public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;
}
