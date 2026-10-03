using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Features.Employees.Services;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Documents;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Features.Employees.Components;

public partial class EmployeeDocumentsPanel
{
    private static readonly IReadOnlyList<string> DocumentGridHeaders = ["الكود", "النوع", "العنوان", "الملف", "الحالة"];
    private const long MaximumBytes = 10 * 1024 * 1024;
    private enum EditorMode { Empty, View, Create, Edit }
    [Parameter, EditorRequired] public Guid EmployeeId { get; set; }
    [Parameter] public EventCallback OnStateChanged { get; set; }
    [Inject] private IEmployeeDocumentClientService Documents { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;
    [Inject] private BrowserFileDownloadService BrowserDownload { get; set; } = default!;

    private Guid _loadedEmployeeId;
    private IReadOnlyList<EmployeeDocumentDto> _items=[];
    private EmployeeDocumentDto? _selected;
    private EditorMode _mode=EditorMode.Empty;
    private bool _loading,_saving;
    private string _documentTypeValue="1";
    private string? _title,_notes,_pendingFileName,_pendingContentType;
    private DateOnly? _issueDate,_expiryDate;
    private byte[]? _pendingContent;
    private static readonly IReadOnlyList<UiSelectOption> DocumentTypeOptions=[new("1","هوية"),new("2","عقد"),new("3","شهادة"),new("4","ترخيص"),new("5","أخرى")];

    public bool CanNew=>!_saving&&!IsEditing;
    public bool CanEdit=>_selected is not null&&!IsEditing&&!_saving;
    public bool CanSave=>IsEditing&&!_saving;
    public bool CanCancelEdit=>IsEditing&&!_saving;
    public bool CanRefresh=>!IsEditing&&!_saving;
    public bool CanToggleStatus=>_selected is not null&&!IsEditing&&!_saving;
    public string ToggleStatusText=>_selected?.IsActive==true?"تعطيل المستند":"تفعيل المستند";
    private bool IsEditing=>_mode is EditorMode.Create or EditorMode.Edit;
    private string EditorTitle=>_mode switch{EditorMode.Create=>"مستند جديد",EditorMode.Edit=>"تعديل بيانات المستند",EditorMode.View=>"تفاصيل المستند",_=>"تفاصيل المستند"};

    protected override async Task OnParametersSetAsync(){if(EmployeeId==Guid.Empty||EmployeeId==_loadedEmployeeId)return;_loadedEmployeeId=EmployeeId;await LoadAsync();}
    public async Task NewAsync(){if(!CanNew)return;_selected=null;_mode=EditorMode.Create;ClearForm();await ChangedAsync();}
    public async Task EditAsync(){if(!CanEdit||_selected is null)return;_mode=EditorMode.Edit;LoadForm(_selected);await ChangedAsync();}
    public async Task CancelEditAsync(){if(!CanCancelEdit)return;if(_selected is null){_mode=EditorMode.Empty;ClearForm();}else{_mode=EditorMode.View;LoadForm(_selected);}await ChangedAsync();}
    public Task RefreshAsync()=>LoadAsync();
    public async Task SaveAsync()
    {
        if(!CanSave||!byte.TryParse(_documentTypeValue,out var type)||string.IsNullOrWhiteSpace(_title)){Snackbar.Error("نوع المستند وعنوانه مطلوبان.");return;}
        if(_expiryDate.HasValue&&_issueDate.HasValue&&_expiryDate<_issueDate){Snackbar.Error("تاريخ انتهاء المستند لا يمكن أن يسبق تاريخ الإصدار.");return;}
        _saving=true;await ChangedAsync();
        try
        {
            if(_mode==EditorMode.Create)
            {
                if(_pendingContent is null||string.IsNullOrWhiteSpace(_pendingFileName)||string.IsNullOrWhiteSpace(_pendingContentType)){Snackbar.Error("اختر ملف المستند أولًا.");return;}
                var result=await Documents.UploadAsync(EmployeeId,type,_title.Trim(),_issueDate,_expiryDate,NullIfEmpty(_notes),_pendingContent,_pendingFileName,_pendingContentType);
                if(!result.Succeeded){if(result.Error is not null)ApiFeedback.Show(result.Error);else ApiFeedback.ShowUnexpected();return;}
            }
            else if(_selected is not null)
            {
                var result=await Documents.UpdateAsync(_selected.Id,new UpdateEmployeeDocumentRequest(type,_title.Trim(),_issueDate,_expiryDate,NullIfEmpty(_notes),_selected.RowVersion));
                if(!result.Succeeded){if(result.Error is not null)ApiFeedback.Show(result.Error);else ApiFeedback.ShowUnexpected();return;}
            }
            _mode=EditorMode.View;await LoadAsync();Snackbar.Success("تم حفظ المستند بنجاح.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
        finally{_saving=false;await ChangedAsync();}
    }
    public async Task ToggleStatusAsync()
    {
        if(!CanToggleStatus||_selected is null)return;
        var result=await Documents.SetStatusAsync(_selected.Id,new SetEmployeeDocumentStatusRequest(!_selected.IsActive,_selected.RowVersion));
        if(!result.Succeeded){if(result.Error is not null)ApiFeedback.Show(result.Error);else ApiFeedback.ShowUnexpected();return;}
        await LoadAsync();Snackbar.Success("تم تحديث حالة المستند.");
    }
    private async Task FileSelectedAsync(InputFileChangeEventArgs args)
    {
        var file=args.File;
        if(file.Size<=0||file.Size>MaximumBytes){Snackbar.Error("حجم الملف غير صالح أو يتجاوز 10 MB.");return;}
        var extension=Path.GetExtension(file.Name).ToLowerInvariant();
        if(extension is not ".pdf" and not ".jpg" and not ".jpeg" and not ".png"){Snackbar.Error("يسمح فقط بملفات PDF أو JPG أو PNG.");return;}
        await using var stream=file.OpenReadStream(MaximumBytes);
        using var memory=new MemoryStream();await stream.CopyToAsync(memory);
        _pendingContent=memory.ToArray();_pendingFileName=Path.GetFileName(file.Name);_pendingContentType=extension==".pdf"?"application/pdf":extension==".png"?"image/png":"image/jpeg";await ChangedAsync();
    }
    private async Task DownloadAsync(EmployeeDocumentDto item)
    {
        try{var bytes=await Documents.DownloadAsync(item.Id);var saved=await BrowserDownload.SaveAsync(bytes,item.OriginalFileName,item.ContentType);if(saved)Snackbar.Success("تم تنزيل المستند.");}
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task LoadAsync()
    {
        _loading=true;await ChangedAsync();
        try{var selectedId=_selected?.Id;_items=await Documents.GetAsync(EmployeeId);_selected=selectedId.HasValue?_items.FirstOrDefault(x=>x.Id==selectedId):_items.FirstOrDefault();if(_selected is not null){_mode=EditorMode.View;LoadForm(_selected);}else{_mode=EditorMode.Empty;ClearForm();}}
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
        finally{_loading=false;await ChangedAsync();}
    }
    private void SelectItem(EmployeeDocumentDto item){if(IsEditing||_saving)return;_selected=item;_mode=EditorMode.View;LoadForm(item);_=ChangedAsync();}
    private void LoadForm(EmployeeDocumentDto x){_documentTypeValue=x.DocumentType.ToString();_title=x.Title;_issueDate=x.IssueDate;_expiryDate=x.ExpiryDate;_notes=x.Notes;_pendingContent=null;_pendingFileName=null;_pendingContentType=null;}
    private void ClearForm(){_documentTypeValue="1";_title=string.Empty;_issueDate=null;_expiryDate=null;_notes=null;_pendingContent=null;_pendingFileName=null;_pendingContentType=null;}
    private Task SetDocumentTypeAsync(string? value){_documentTypeValue=value??"1";return Task.CompletedTask;}
    private Task ChangedAsync()=>OnStateChanged.InvokeAsync();
    private static string TypeName(byte value)=>value switch{1=>"هوية",2=>"عقد",3=>"شهادة",4=>"ترخيص",5=>"أخرى",_=>"—"};
    private static string FormatFileSize(long bytes)=>bytes<1024?$"{bytes} B":bytes<1024*1024?$"{bytes/1024d:0.0} KB":$"{bytes/(1024d*1024d):0.0} MB";
    private static string? NullIfEmpty(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
