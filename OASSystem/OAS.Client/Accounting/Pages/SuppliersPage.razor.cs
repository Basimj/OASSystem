using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Services.Browser;
using OAS.Client.Services.Http;
using OAS.Contracts.Accounting.Suppliers;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Spreadsheets;
using OAS.UiLib.Components.Spreadsheets;
using OAS.UiLib.Core.Models.Accounting;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Accounting.Pages;

public partial class SuppliersPage
{
    [Inject] private IAccountingClientService Accounting { get; set; } = default!;
    [Inject] private AccountingSpreadsheetClient Spreadsheets { get; set; } = default!;
    [Inject] private BrowserFileDownloadService Download { get; set; } = default!;
    [Inject] private IApiFeedbackService ApiFeedback { get; set; } = default!;
    [Inject] private IUiSnackbarService Snackbar { get; set; } = default!;

    private const int PageSize = 25;
    private bool _busy, _editorOpen, _isNew, _importOpen, _spreadsheetBusy;
    private int _pageNumber = 1, _totalPages = 1;
    private long _totalCount, _importKey;
    private string _search = string.Empty, _filter = "all", _fileName = string.Empty;
    private byte[]? _fileBytes;
    private SpreadsheetPreview? _preview;
    private UiAccountingPartyFormModel _form = new();
    private IReadOnlyList<UiAccountingPartyListItem> _items = [];
    private IReadOnlyList<UiAccountingPartyParentOption> _parents = [];
    private UiSpreadsheetPreview? UiPreview => _preview is null ? null : new UiSpreadsheetPreview(_preview.Rows.Select(x => new UiSpreadsheetRow(x.Sheet,x.RowNumber,x.Values,x.Errors,x.Warnings)).ToArray(),_preview.ValidRows,_preview.InvalidRows,_preview.WarningRows,_preview.CanImport,_preview.ImportedRecords);

    protected override async Task OnInitializedAsync(){await LoadParentsAsync();await LoadAsync();}
    private Task SetSearch(string? v){_search=v??"";return Task.CompletedTask;}
    private async Task SearchAsync(string? v){_search=v??"";_pageNumber=1;await LoadAsync();}
    private async Task FilterChangedAsync(string? value){_filter=string.IsNullOrWhiteSpace(value)?"all":value;_pageNumber=1;await LoadAsync();}
    private async Task LoadAsync()=>await RunAsync(async()=>{var page=await Accounting.GetSuppliersPageAsync(new PageRequest{PageNumber=_pageNumber,PageSize=PageSize,Search=_search,SortBy="SupplierCode"},_filter);_pageNumber=page.PageNumber<=0?1:page.PageNumber;_totalPages=Math.Max(1,page.TotalPages);_totalCount=page.TotalCount;_items=page.Items.Select(ToListItem).ToArray();});
    private async Task LoadParentsAsync(){var values=await Accounting.GetSupplierAccountParentsAsync()??[];_parents=values.Select(x=>new UiAccountingPartyParentOption(x.Id,x.Code,x.NameAr)).ToArray();}
    private async Task NewAsync()=>await RunAsync(async()=>{if(_parents.Count==0)await LoadParentsAsync();var reserved=await Accounting.ReserveSupplierCodeAsync();_form=new UiAccountingPartyFormModel{Code=reserved?.SupplierCode??"",EntityType="2",SupplierScope="1",PreferredContactMethod="2",IsActive=true};if(_parents.Count==1)_form.ParentAccountId=_parents[0].Id.ToString("D");_isNew=true;_editorOpen=true;});
    private async Task OpenAsync(Guid id)=>await RunAsync(async()=>{var dto=await Accounting.GetSupplierByIdAsync(id);if(dto is null){Snackbar.Warning("تعذر العثور على المورد.");return;}_form=FromDto(dto);_isNew=false;_editorOpen=true;});

    private async Task SaveAsync()=>await RunAsync(async()=>
    {
        if(string.IsNullOrWhiteSpace(_form.NameAr)){Snackbar.Warning("الاسم العربي مطلوب.");return;}
        if(!Enum.TryParse<PartyEntityType>(_form.EntityType,out var entityType)||entityType==PartyEntityType.Unknown){Snackbar.Warning("اختر نوع الكيان.");return;}
        if(!Enum.TryParse<SupplierScope>(_form.SupplierScope,out var scope)||scope==SupplierScope.Unknown){Snackbar.Warning("اختر نطاق المورد.");return;}
        if(!Enum.TryParse<ContactMethod>(_form.PreferredContactMethod,out var contact)||contact==ContactMethod.Unspecified){Snackbar.Warning("اختر وسيلة التواصل المفضلة.");return;}
        SupplierDto? result;
        if(_isNew)
        {
            if(!Guid.TryParse(_form.ParentAccountId,out var parent)){Snackbar.Warning("اختر حساب الموردين الرئيسي.");return;}
            result=await Accounting.CreateSupplierAsync(new CreateSupplierRequest(_form.Code,parent,entityType,scope,_form.NameAr.Trim(),N(_form.NameEn),N(_form.TradeName),N(_form.NationalId),N(_form.CommercialRegistrationNo),N(_form.TaxNumber),N(_form.ContactPersonName),N(_form.ContactPersonTitle),N(_form.Phone),N(_form.Mobile),N(_form.AlternatePhone),N(_form.WhatsAppNumber),N(_form.Email),N(_form.Website),contact,N(_form.Country),N(_form.Governorate),N(_form.City),N(_form.District),N(_form.Street),N(_form.Building),N(_form.PostalCode),N(_form.AddressDetails),_form.CreditLimit,_form.PaymentTermDays,_form.DefaultLeadTimeDays,_form.Since,_form.IsActive,N(_form.Notes)));
        }
        else
        {
            if(!_form.Id.HasValue||string.IsNullOrWhiteSpace(_form.RowVersion))return;
            result=await Accounting.UpdateSupplierAsync(_form.Id.Value,new UpdateSupplierRequest(entityType,scope,_form.NameAr.Trim(),N(_form.NameEn),N(_form.TradeName),N(_form.NationalId),N(_form.CommercialRegistrationNo),N(_form.TaxNumber),N(_form.ContactPersonName),N(_form.ContactPersonTitle),N(_form.Phone),N(_form.Mobile),N(_form.AlternatePhone),N(_form.WhatsAppNumber),N(_form.Email),N(_form.Website),contact,N(_form.Country),N(_form.Governorate),N(_form.City),N(_form.District),N(_form.Street),N(_form.Building),N(_form.PostalCode),N(_form.AddressDetails),_form.CreditLimit,_form.PaymentTermDays,_form.DefaultLeadTimeDays,_form.Since,N(_form.Notes),_form.RowVersion));
        }
        if(result is not null){_form=FromDto(result);_isNew=false;Snackbar.Success("تم حفظ المورد بنجاح.");await LoadAsync();}
    });
    private Task CancelAsync(){_editorOpen=false;_isNew=false;_form=new();return Task.CompletedTask;}
    private async Task ToggleStatusAsync()=>await RunAsync(async()=>{if(!_form.Id.HasValue||string.IsNullOrWhiteSpace(_form.RowVersion))return;var result=await Accounting.SetSupplierStatusAsync(_form.Id.Value,new SetSupplierStatusRequest(!_form.IsActive,_form.RowVersion));if(result is not null){_form=FromDto(result);await LoadAsync();Snackbar.Success(result.IsActive?"تم تفعيل المورد.":"تم تعطيل المورد.");}});
    private async Task PreviousPageAsync(){if(_pageNumber>1){_pageNumber--;await LoadAsync();}}
    private async Task NextPageAsync(){if(_pageNumber<_totalPages){_pageNumber++;await LoadAsync();}}

    private Task OpenImport(){_preview=null;_fileBytes=null;_fileName="";_importKey++;_importOpen=true;return Task.CompletedTask;}
    private Task CloseImport(){if(!_spreadsheetBusy)_importOpen=false;return Task.CompletedTask;}
    private Task ClearImport(){if(!_spreadsheetBusy){_preview=null;_fileBytes=null;_fileName="";}return Task.CompletedTask;}
    private async Task PreviewImportAsync(IBrowserFile file)=>await RunSpreadsheetAsync(async()=>{_preview=null;_fileBytes=null;_fileName="";if(!file.Name.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase)||file.Size>10*1024*1024){_preview=InvalidFile();return;}using var ms=new MemoryStream();await using var stream=file.OpenReadStream(10*1024*1024);await stream.CopyToAsync(ms);_fileBytes=ms.ToArray();_fileName=file.Name;_preview=await Spreadsheets.UploadAsync("suppliers",_fileBytes,_fileName,false);});
    private async Task ConfirmImportAsync()=>await RunSpreadsheetAsync(async()=>{if(_fileBytes is null||_preview is null||!_preview.CanImport)return;_preview=await Spreadsheets.UploadAsync("suppliers",_fileBytes,_fileName,true);if(_preview.ImportedRecords>0){Snackbar.Success($"تم استيراد {_preview.ImportedRecords} مورد.");await LoadAsync();}});
    private async Task DownloadTemplateAsync()=>await RunSpreadsheetAsync(async()=>await Download.SaveAsync(await Spreadsheets.TemplateAsync("suppliers"),"suppliers-template.xlsx","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
    private async Task ExportAsync()=>await RunSpreadsheetAsync(async()=>await Download.SaveAsync(await Spreadsheets.ExportAsync("suppliers",_search,null,_filter),"suppliers.xlsx","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
    private async Task RunAsync(Func<Task> action){if(_busy)return;_busy=true;try{await action();}catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}finally{_busy=false;}}
    private async Task RunSpreadsheetAsync(Func<Task> action){if(_spreadsheetBusy)return;_spreadsheetBusy=true;try{await action();}catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}finally{_spreadsheetBusy=false;}}
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
    private static SpreadsheetPreview InvalidFile()=>new([new SpreadsheetRowResult("الملف",1,new Dictionary<string,string>(),["اختر ملف xlsx بحجم لا يتجاوز 10 MB."],[])]);
    private static UiAccountingPartyListItem ToListItem(SupplierDto x)=>new(x.Id,x.SupplierCode,x.AccountCode,x.NameAr,x.TradeName??x.NameEn,x.Mobile??x.Phone,string.Join(" / ",new[]{x.City,x.Country}.Where(v=>!string.IsNullOrWhiteSpace(v))),x.TaxNumber??x.CommercialRegistrationNo,$"{(x.EntityType switch { PartyEntityType.Individual => "فرد", PartyEntityType.Organization => "منشأة", _ => "غير محدد" })} · {(x.SupplierScope switch { SupplierScope.Local => "محلي", SupplierScope.International => "دولي", _ => "غير محدد" })}",$"{x.PaymentTermDays} يوم · توريد {x.DefaultLeadTimeDays?.ToString()??"—"}",x.IsActive);
    private static UiAccountingPartyFormModel FromDto(SupplierDto x)=>new(){Id=x.Id,Code=x.SupplierCode,AccountCode=x.AccountCode,ParentAccountId=x.ParentAccountId?.ToString("D")??"",EntityType=((byte)x.EntityType).ToString(),SupplierScope=((byte)x.SupplierScope).ToString(),NameAr=x.NameAr,NameEn=x.NameEn,TradeName=x.TradeName,NationalId=x.NationalId,CommercialRegistrationNo=x.CommercialRegistrationNo,TaxNumber=x.TaxNumber,ContactPersonName=x.ContactPersonName,ContactPersonTitle=x.ContactPersonTitle,Phone=x.Phone,Mobile=x.Mobile,AlternatePhone=x.AlternatePhone,WhatsAppNumber=x.WhatsAppNumber,Email=x.Email,Website=x.Website,PreferredContactMethod=((byte)x.PreferredContactMethod).ToString(),Country=x.Country,Governorate=x.Governorate,City=x.City,District=x.District,Street=x.Street,Building=x.Building,PostalCode=x.PostalCode,AddressDetails=x.AddressDetails,CreditLimit=x.CreditLimit,PaymentTermDays=x.PaymentTermDays,DefaultLeadTimeDays=x.DefaultLeadTimeDays,Since=x.SupplierSince,IsActive=x.IsActive,Notes=x.Notes,RowVersion=x.RowVersion};
}
