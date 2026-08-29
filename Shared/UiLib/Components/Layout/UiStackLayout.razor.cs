using Microsoft.AspNetCore.Components;
using OAS.UiLib.Core.Enums;

namespace OAS.UiLib.Components.Layout;

public partial class UiStackLayout
{
    [Parameter] public StackGap Gap { get; set; } = StackGap.Medium;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    private string CssClass => $"ui-stack ui-stack--{Gap.ToString().ToLowerInvariant()}";
}
