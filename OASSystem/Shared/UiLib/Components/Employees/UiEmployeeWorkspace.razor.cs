using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Employees;

public partial class UiEmployeeWorkspace
{
    [Parameter] public RenderFragment? Actions { get; set; }
    [Parameter] public RenderFragment? Tabs { get; set; }
    [Parameter, EditorRequired] public RenderFragment? Content { get; set; }
}
