using Microsoft.AspNetCore.Components;

namespace OAS.UiLib.Components.Employees;

public partial class UiEmployeeListSurface
{
    [Parameter] public bool Loading { get; set; }
    [Parameter] public bool IsEmpty { get; set; }
    [Parameter] public string LoadingText { get; set; } = "جاري تحميل الموظفين...";
    [Parameter] public string EmptyTitle { get; set; } = "لا توجد نتائج";
    [Parameter] public string EmptyText { get; set; } = "لم يتم العثور على موظفين مطابقين للبحث أو الفلتر الحالي.";
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }
}
