using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Layout;

public partial class UiSystemBrand
{
    [Parameter] public string Name { get; set; } = "OAS System";
    [Parameter] public string? LogoUrl { get; set; }
    [Parameter] public string IconCssClass { get; set; } = "fa-solid fa-cube";
}
