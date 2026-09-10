using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Employees;

public partial class UiEmployeeImportChoiceDialog
{
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnInternalEntry { get; set; }
    [Parameter] public EventCallback OnExcelImport { get; set; }
    private Task CloseAsync() => OnClose.InvokeAsync();
    private Task InternalEntryAsync() => OnInternalEntry.InvokeAsync();
    private Task ExcelAsync() => OnExcelImport.InvokeAsync();
}
