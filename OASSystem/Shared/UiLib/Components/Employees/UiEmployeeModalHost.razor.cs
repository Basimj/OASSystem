using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Employees;

public partial class UiEmployeeModalHost
{
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public bool Wide { get; set; }
    private string SizeClass => Wide ? "ui-employee-modal-host__dialog--wide" : "ui-employee-modal-host__dialog--normal";
    private Task CloseAsync() => OnClose.InvokeAsync();
}
