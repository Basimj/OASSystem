using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace OAS.UiLib.Components.Forms;

public partial class UiForm
{
    [Parameter, EditorRequired] public object Model { get; set; } = default!;
    [Parameter] public EventCallback OnSubmit { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    private Task HandleSubmitAsync(EditContext _) => OnSubmit.InvokeAsync();
}
