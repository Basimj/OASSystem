using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Employees;

public partial class UiEmployeeExportChoiceDialog
{
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnExport { get; set; }
    [Parameter] public EventCallback OnDownloadTemplate { get; set; }
    private Task CloseAsync() => OnClose.InvokeAsync();
    private Task ExportAsync() => OnExport.InvokeAsync();
    private Task TemplateAsync() => OnDownloadTemplate.InvokeAsync();
}
