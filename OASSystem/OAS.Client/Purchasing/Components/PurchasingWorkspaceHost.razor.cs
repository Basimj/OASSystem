using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Inventory.Services;
using OAS.Client.Purchasing.Common;
using OAS.Client.Purchasing.Services;
using OAS.Client.Purchasing.Workspace;
using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Contracts.Purchasing.PurchaseRequests;
using OAS.Contracts.Purchasing.SupplierCatalog;
using OAS.Contracts.Sales.Enums;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Purchasing;
using OAS.UiLib.Services.Dialogs;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Purchasing.Components;

public partial class PurchasingWorkspaceHost
{
    private const int PageSize=24;
    [Parameter,EditorRequired] public PurchasingEntityType Section {get;set;}
    [Inject] private IPurchasingClientService Purchasing {get;set;}=default!;
    [Inject] private IInventoryClientService Inventory {get;set;}=default!;
    [Inject] private IAccountingClientService Accounting {get;set;}=default!;
    [Inject] private IPurchasingWorkspaceState Workspace {get;set;}=default!;
    [Inject] private IApiFeedbackService ApiFeedback {get;set;}=default!;
    [Inject] private IUiSnackbarService Snackbar {get;set;}=default!;
    [Inject] private IUiDialogService Dialog {get;set;}=default!;
    [Inject] private NavigationManager Navigation {get;set;}=default!;

    private PurchasingEntityType? _initializedSection;
    private bool _isLoadingList;
    private string? _search;
    private int _pageNumber=1;
    private int _totalPages;
    private long _totalCount;
    private IReadOnlyList<UiPurchasingListItem> CurrentListItems=[];

    private PurchasingTabState? ActiveTab=>Workspace.Find(Workspace.ActiveTabId);
    private bool IsListTab=>ActiveTab is null || ActiveTab.IsListTab;
    private bool IsEditable=>ActiveTab is { } t && !t.IsListTab && (t.IsNew||t.IsEditMode) && IsDomainEditable(t.Model);
    private bool CanBeginEdit=>ActiveTab is {IsListTab:false,IsNew:false,IsEditMode:false} t && IsDomainEditable(t.Model);
    private bool CanSave=>ActiveTab is {IsListTab:false} t && (t.IsNew||t.IsEditMode) && IsDomainEditable(t.Model);
    private bool CanCancelEdit=>ActiveTab is {IsListTab:false} t && (t.IsNew||t.IsEditMode);
    private string TabsAriaLabel=>$"تبويبات {ListTitle(Section)}";

    private IReadOnlyList<UiApplicationTabItem> WorkspaceTabs=>Workspace.VisibleTabs(Section)
        .Select(t=>new UiApplicationTabItem(t.TabId,t.Title,Route(Section),Icon(Section),Workspace.ActiveTabId==t.TabId,t.CanClose)).ToArray();

    protected override async Task OnParametersSetAsync()
    {
        if(_initializedSection==Section)return;
        _initializedSection=Section;
        var activeId=Workspace.ActiveTab?.EntityType==Section?Workspace.ActiveTabId:Guid.Empty;
        Workspace.OpenList(Section);
        if(activeId!=Guid.Empty)Workspace.ActiveTabId=activeId;
        _search=null;_pageNumber=1;
        if(IsListTab)await LoadListAsync(); else if(ActiveTab is { } tab)await EnsureLoadedAsync(tab);
    }

    private async Task LoadListAsync()
    {
        if(_isLoadingList)return;_isLoadingList=true;
        try
        {
            var request=new PageRequest{PageNumber=_pageNumber,PageSize=PageSize,Search=string.IsNullOrWhiteSpace(_search)?null:_search.Trim(),SortBy=SortField(Section),SortDirection=SortDirection.Descending};
            switch(Section)
            {
                case PurchasingEntityType.SupplierCatalog:
                    var c=await Purchasing.GetSupplierCatalogAsync(request);SetPage(c.PageNumber,c.TotalPages,c.TotalCount,c.Items.Select(x=>new UiPurchasingListItem(x.Id,x.SupplierProductCode??x.ProductCode??"—",x.ProductName??x.SupplierProductName??"صنف",x.SupplierName,x.IsActive?"فعال":"غير فعال",x.PurchaseUnitName,"fa-solid fa-box-open")).ToArray());break;
                case PurchasingEntityType.PurchaseRequests:
                    var r=await Purchasing.GetPurchaseRequestsAsync(request);SetPage(r.PageNumber,r.TotalPages,r.TotalCount,r.Items.Select(x=>new UiPurchasingListItem(x.Id,x.RequestCode,x.Reason??x.WarehouseName??"طلب شراء",x.WarehouseName,PurchasingArabicPresenter.RequestStatus(x.Status),x.RequestDate.ToString("yyyy-MM-dd"),"fa-solid fa-clipboard-list")).ToArray());break;
                case PurchasingEntityType.PurchaseOrders:
                    var o=await Purchasing.GetPurchaseOrdersAsync(request);SetPage(o.PageNumber,o.TotalPages,o.TotalCount,o.Items.Select(x=>new UiPurchasingListItem(x.Id,x.PurchaseOrderCode,x.SupplierName??"مورد",x.DestinationWarehouseName,PurchasingArabicPresenter.OrderStatus(x.Status),$"{x.TotalAmount:N2} {x.CurrencyCode}","fa-solid fa-file-signature")).ToArray());break;
                case PurchasingEntityType.PurchaseReceipts:
                    var gr=await Purchasing.GetPurchaseReceiptsAsync(request);SetPage(gr.PageNumber,gr.TotalPages,gr.TotalCount,gr.Items.Select(x=>new UiPurchasingListItem(x.Id,x.ReceiptCode,x.SupplierName??"استلام",x.PurchaseOrderCode,PurchasingArabicPresenter.ReceiptStatus(x.Status),x.ReceiptDate.ToString("yyyy-MM-dd"),"fa-solid fa-truck-ramp-box")).ToArray());break;
                case PurchasingEntityType.PurchaseInvoices:
                    var pi=await Purchasing.GetPurchaseInvoicesAsync(request);SetPage(pi.PageNumber,pi.TotalPages,pi.TotalCount,pi.Items.Select(x=>new UiPurchasingListItem(x.Id,x.PurchaseInvoiceCode,x.SupplierName??"فاتورة مورد",x.SupplierInvoiceCode,PurchasingArabicPresenter.InvoiceStatus(x.Status),$"{x.TotalAmount:N2} {x.CurrencyCode}","fa-solid fa-file-invoice-dollar")).ToArray());break;
            }
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
        finally{_isLoadingList=false;}
    }
    private void SetPage(int page,int pages,long total,IReadOnlyList<UiPurchasingListItem> items){_pageNumber=page;_totalPages=pages;_totalCount=total;CurrentListItems=items;}

    private async Task OpenNewAsync(MouseEventArgs _)
    {
        var tab=Workspace.OpenNew(Section,NewTitle(Section));
        tab.Model=Section switch
        {
            PurchasingEntityType.SupplierCatalog=>new UiSupplierCatalogEditorModel(),
            PurchasingEntityType.PurchaseRequests=>NewRequest(),
            PurchasingEntityType.PurchaseOrders=>NewOrder(),
            PurchasingEntityType.PurchaseReceipts=>new UiPurchaseReceiptEditorModel(),
            PurchasingEntityType.PurchaseInvoices=>NewInvoice(),
            _=>null
        };
        tab.IsEditMode=true;tab.IsDirty=false;await InvokeAsync(StateHasChanged);
    }
    private static UiPurchaseRequestEditorModel NewRequest(){var m=new UiPurchaseRequestEditorModel();m.Lines.Add(new(){LineSequence=1});return m;}
    private static UiPurchaseOrderEditorModel NewOrder(){var m=new UiPurchaseOrderEditorModel();m.Lines.Add(new(){LineSequence=1});return m;}
    private static UiPurchaseInvoiceEditorModel NewInvoice(){var m=new UiPurchaseInvoiceEditorModel();m.Lines.Add(new(){LineSequence=1});return m;}

    private async Task OpenRecordAsync(Guid id)
    {
        var tab=Workspace.OpenRecord(Section,id,"جاري التحميل...");await EnsureLoadedAsync(tab);await InvokeAsync(StateHasChanged);
    }
    private async Task EnsureLoadedAsync(PurchasingTabState tab,bool force=false)
    {
        if(tab.IsNew||tab.EntityId is not Guid id||tab.IsLoading||(!force&&tab.Model is not null))return;tab.IsLoading=true;
        try
        {
            tab.Model=tab.EntityType switch
            {
                PurchasingEntityType.SupplierCatalog=>Map((await Purchasing.GetSupplierCatalogItemAsync(id))!),
                PurchasingEntityType.PurchaseRequests=>Map((await Purchasing.GetPurchaseRequestAsync(id))!),
                PurchasingEntityType.PurchaseOrders=>Map((await Purchasing.GetPurchaseOrderAsync(id))!),
                PurchasingEntityType.PurchaseReceipts=>Map((await Purchasing.GetPurchaseReceiptAsync(id))!),
                PurchasingEntityType.PurchaseInvoices=>Map((await Purchasing.GetPurchaseInvoiceAsync(id))!),
                _=>null
            };
            if(tab.Model is null)throw new InvalidOperationException();
            tab.Title=ModelTitle(tab.Model);tab.IsDirty=false;tab.IsEditMode=false;
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);Workspace.Close(tab.TabId);}catch{ApiFeedback.ShowUnexpected();Workspace.Close(tab.TabId);}
        finally{tab.IsLoading=false;}
    }

    private Task EditorChangedAsync(){if(ActiveTab is { } t)t.IsDirty=true;StateHasChanged();return Task.CompletedTask;}
    private Task BeginEditAsync(MouseEventArgs _){if(ActiveTab is { } t)t.IsEditMode=true;StateHasChanged();return Task.CompletedTask;}
    private async Task CancelEditAsync(MouseEventArgs _)
    {
        if(ActiveTab is not { } tab)return;
        if(tab.IsNew){if(!await ConfirmDiscardAsync(tab))return;Workspace.Close(tab.TabId);return;}
        if(tab.IsDirty&&!await Dialog.ConfirmAsync("إلغاء التعديلات","سيتم تجاهل التغييرات غير المحفوظة.",AlertTone.Warning,"تجاهل","رجوع"))return;
        tab.Model=null;tab.IsEditMode=false;tab.IsDirty=false;await EnsureLoadedAsync(tab,true);await InvokeAsync(StateHasChanged);
    }
    private async Task RefreshActiveAsync(MouseEventArgs _){if(ActiveTab is not {IsNew:false} tab)return;if(tab.IsDirty&&!await ConfirmDiscardAsync(tab))return;tab.Model=null;await EnsureLoadedAsync(tab,true);await InvokeAsync(StateHasChanged);}
    private async Task CloseActiveAsync(MouseEventArgs _){if(ActiveTab is { } tab)await CloseTabAsync(tab.TabId);}
    private async Task SelectTabAsync(Guid id){Workspace.ActiveTabId=id;if(Workspace.Find(id) is { } t)await EnsureLoadedAsync(t);if(IsListTab)await LoadListAsync();}
    private async Task CloseTabAsync(Guid id){var tab=Workspace.Find(id);if(tab is null||!tab.CanClose)return;if(!await ConfirmDiscardAsync(tab))return;Workspace.Close(id);await InvokeAsync(StateHasChanged);}
    private async Task CloseOtherTabsAsync(Guid id){var keep=Workspace.Find(id);foreach(var tab in Workspace.VisibleTabs(Section).Where(x=>x.CanClose&&x.TabId!=id).ToArray()){if(!await ConfirmDiscardAsync(tab))return;}foreach(var tab in Workspace.VisibleTabs(Section).Where(x=>x.CanClose&&x.TabId!=id).ToArray())Workspace.Close(tab.TabId);Workspace.ActiveTabId=keep?.TabId??Workspace.OpenList(Section).TabId;}
    private async Task CloseAllTabsAsync(){var tabs=Workspace.VisibleTabs(Section).Where(x=>x.CanClose).ToArray();foreach(var t in tabs)if(!await ConfirmDiscardAsync(t))return;foreach(var t in tabs)Workspace.Close(t.TabId);Workspace.OpenList(Section);}
    private async Task<bool> ConfirmDiscardAsync(PurchasingTabState tab)=>!tab.IsDirty||await Dialog.ConfirmAsync("تغييرات غير محفوظة",$"توجد تغييرات غير محفوظة في «{tab.Title}». هل تريد تجاهلها؟",AlertTone.Warning,"تجاهل وإغلاق","رجوع");

    private async Task SaveAsync(MouseEventArgs _)
    {
        var tab=ActiveTab;if(tab is null||tab.IsSaving)return;tab.IsSaving=true;
        try
        {
            object? saved=tab.Model switch
            {
                UiSupplierCatalogEditorModel m=>await SaveCatalogAsync(m),
                UiPurchaseRequestEditorModel m=>await SaveRequestAsync(m),
                UiPurchaseOrderEditorModel m=>await SaveOrderAsync(m),
                UiPurchaseReceiptEditorModel m=>await SaveReceiptAsync(m),
                UiPurchaseInvoiceEditorModel m=>await SaveInvoiceAsync(m),
                _=>null
            };
            if(saved is null)return;
            tab.Model=saved;tab.EntityId=ModelId(saved);tab.Title=ModelTitle(saved);tab.IsEditMode=false;tab.IsDirty=false;Snackbar.Success("تم حفظ البيانات بنجاح.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
        finally{tab.IsSaving=false;}
    }

    private async Task<object?> SaveCatalogAsync(UiSupplierCatalogEditorModel m)
    {
        if(m.SupplierId is not Guid supplier||m.ProductVariantId is not Guid product||m.PurchaseUnitId is not Guid unit){Snackbar.Warning("حدد المورد والمنتج ووحدة الشراء.");return null;}
        if(!m.Id.HasValue)return Map((await Purchasing.CreateSupplierCatalogItemAsync(new(supplier,product,m.SupplierProductCode,m.SupplierProductName,unit,m.UnitConversionFactor,m.LeadTimeDays,m.MinimumOrderQuantity,m.IsPreferred,m.IsActive)))!);
        return Map((await Purchasing.UpdateSupplierCatalogItemAsync(m.Id.Value,new(m.SupplierProductCode,m.SupplierProductName,unit,m.UnitConversionFactor,m.LeadTimeDays,m.MinimumOrderQuantity,m.IsPreferred,m.IsActive,m.RowVersion??string.Empty)))!);
    }
    private async Task<object?> SaveRequestAsync(UiPurchaseRequestEditorModel m)
    {
        if(m.WarehouseId is not Guid warehouse||m.Lines.Count==0||m.Lines.Any(x=>!x.ProductVariantId.HasValue)){Snackbar.Warning("حدد المخزن ومنتجًا صالحًا لكل بند.");return null;}
        if(!m.Id.HasValue)
        {
            var lines=m.Lines.Select(x=>new CreatePurchaseRequestLineRequest(x.LineSequence,x.ProductVariantId!.Value,x.RequestedQuantity,x.RequiredDate,null,x.PreferredSupplierId,x.Notes)).ToArray();
            return Map((await Purchasing.CreatePurchaseRequestAsync(new((PurchaseRequestType)m.RequestType,warehouse,m.CustomerOrderId,m.RequestDate,m.RequiredDate,m.Reason,m.Notes,lines)))!);
        }
        var updates=m.Lines.Select(x=>new UpdatePurchaseRequestLineRequest(x.Id,x.LineSequence,x.ProductVariantId!.Value,x.RequestedQuantity,x.RequiredDate,null,x.PreferredSupplierId,x.Notes,x.RowVersion)).ToArray();
        return Map((await Purchasing.UpdatePurchaseRequestAsync(m.Id.Value,new((PurchaseRequestType)m.RequestType,warehouse,m.CustomerOrderId,m.RequestDate,m.RequiredDate,m.Reason,m.Notes,updates,m.RowVersion??string.Empty)))!);
    }
    private async Task<object?> SaveOrderAsync(UiPurchaseOrderEditorModel m)
    {
        if(m.SupplierId is not Guid supplier||m.DestinationWarehouseId is not Guid warehouse||m.CurrencyId is not Guid currency||m.Lines.Count==0||m.Lines.Any(x=>!x.ProductVariantId.HasValue||!x.PurchaseUnitId.HasValue)){Snackbar.Warning("أكمل المورد والمخزن والعملة ووحدة الشراء لكل بند قبل الحفظ.");return null;}
        if(m.ExchangeRate<=0m){Snackbar.Warning("سعر الصرف يجب أن يكون أكبر من صفر.");return null;}
        if(m.Lines.Any(x=>x.UnitConversionFactor<=0m||x.OrderedQuantity<=0m)){Snackbar.Warning("تحقق من معامل التحويل والكمية في جميع بنود أمر الشراء.");return null;}
        if(m.Lines.Any(x=>x.Sources.Any(s=>s.AllocatedQuantity<=0m))){Snackbar.Warning("الكمية المخصصة من طلب الشراء يجب أن تكون أكبر من صفر.");return null;}
        if(!m.Id.HasValue)
        {
            var lines=m.Lines.Select(x=>new CreatePurchaseOrderLineRequest(x.LineSequence,x.ProductVariantId!.Value,x.SupplierCatalogItemId,x.PurchaseUnitId!.Value,x.UnitConversionFactor,x.OrderedQuantity,x.UnitPrice,x.DiscountAmount,x.TaxRate,x.ExpectedDeliveryDate,x.Notes,x.Sources.Select(s=>new CreatePurchaseOrderLineSourceRequest(s.PurchaseRequestLineId,s.AllocatedQuantity)).ToArray())).ToArray();
            return Map((await Purchasing.CreatePurchaseOrderAsync(new(supplier,warehouse,m.OrderDate,m.ExpectedDeliveryDate,currency,m.ExchangeRate,m.ExchangeRateDate,(TaxCalculationMode)m.TaxCalculationMode,m.PaymentTermDays,m.Notes,lines)))!);
        }
        var updates=m.Lines.Select(x=>new UpdatePurchaseOrderLineRequest(x.Id,x.LineSequence,x.ProductVariantId!.Value,x.SupplierCatalogItemId,x.PurchaseUnitId!.Value,x.UnitConversionFactor,x.OrderedQuantity,x.UnitPrice,x.DiscountAmount,x.TaxRate,x.ExpectedDeliveryDate,x.Notes,x.Sources.Select(s=>new UpdatePurchaseOrderLineSourceRequest(s.Id,s.PurchaseRequestLineId,s.AllocatedQuantity,s.RowVersion)).ToArray(),x.RowVersion)).ToArray();
        return Map((await Purchasing.UpdatePurchaseOrderAsync(m.Id.Value,new(supplier,warehouse,m.OrderDate,m.ExpectedDeliveryDate,currency,m.ExchangeRate,m.ExchangeRateDate,(TaxCalculationMode)m.TaxCalculationMode,m.PaymentTermDays,m.Notes,updates,m.RowVersion??string.Empty)))!);
    }
    private async Task<object?> SaveReceiptAsync(UiPurchaseReceiptEditorModel m)
    {
        if(m.PurchaseOrderId is not Guid po||m.Lines.Count==0){Snackbar.Warning("حدد أمر شراء وأدخل كميات الاستلام.");return null;}
        if(!m.Id.HasValue)
        {
            var lines=m.Lines.Select(x=>new CreatePurchaseReceiptLineRequest(x.PurchaseOrderLineId,x.LineSequence,x.ReceivedQuantity,x.AcceptedQuantity,x.RejectedQuantity,x.ActualUnitCost,x.ExpiryDate,x.BatchCode,x.Notes)).ToArray();
            return Map((await Purchasing.CreatePurchaseReceiptAsync(new(po,m.ReceiptDate,m.PostingDate,m.SupplierDeliveryCode,m.Notes,lines)))!);
        }
        var updates=m.Lines.Select(x=>new UpdatePurchaseReceiptLineRequest(x.Id,x.PurchaseOrderLineId,x.LineSequence,x.ReceivedQuantity,x.AcceptedQuantity,x.RejectedQuantity,x.ActualUnitCost,x.ExpiryDate,x.BatchCode,x.Notes,x.RowVersion)).ToArray();
        return Map((await Purchasing.UpdatePurchaseReceiptAsync(m.Id.Value,new(m.ReceiptDate,m.PostingDate,m.SupplierDeliveryCode,m.Notes,updates,m.RowVersion??string.Empty)))!);
    }
    private async Task<object?> SaveInvoiceAsync(UiPurchaseInvoiceEditorModel m)
    {
        if(m.SupplierId is not Guid supplier||m.CurrencyId is not Guid currency||m.Lines.Count==0||m.Lines.Any(x=>!x.ProductVariantId.HasValue)){Snackbar.Warning("أكمل المورد والعملة ومنتجات الفاتورة.");return null;}
        if(!m.Id.HasValue)
        {
            var lines=m.Lines.Select(x=>new CreatePurchaseInvoiceLineRequest(x.LineSequence,x.PurchaseOrderLineId,x.ProductVariantId!.Value,x.Quantity,x.UnitPrice,x.DiscountAmount,x.TaxRate)).ToArray();
            return Map((await Purchasing.CreatePurchaseInvoiceAsync(new(m.SupplierInvoiceCode,supplier,m.InvoiceDate,m.PostingDate,currency,m.ExchangeRate,m.ExchangeRateDate,(TaxCalculationMode)m.TaxCalculationMode,m.Notes,lines)))!);
        }
        var updates=m.Lines.Select(x=>new UpdatePurchaseInvoiceLineRequest(x.Id,x.LineSequence,x.PurchaseOrderLineId,x.ProductVariantId!.Value,x.Quantity,x.UnitPrice,x.DiscountAmount,x.TaxRate,x.RowVersion)).ToArray();
        return Map((await Purchasing.UpdatePurchaseInvoiceAsync(m.Id.Value,new(m.SupplierInvoiceCode,supplier,m.InvoiceDate,m.PostingDate,currency,m.ExchangeRate,m.ExchangeRateDate,(TaxCalculationMode)m.TaxCalculationMode,m.Notes,updates,m.RowVersion??string.Empty)))!);
    }

    private async Task SearchTextChanged(string? value){_search=value;await Task.CompletedTask;}
    private async Task SearchAsync(string? value){_search=value;_pageNumber=1;await LoadListAsync();}
    private async Task RefreshListAsync(MouseEventArgs _)=>await LoadListAsync();
    private async Task PreviousPageAsync(){if(_pageNumber<=1)return;_pageNumber--;await LoadListAsync();}
    private async Task NextPageAsync(){if(_pageNumber>=_totalPages)return;_pageNumber++;await LoadListAsync();}

    private static bool IsDomainEditable(object? model)=>model switch
    {
        UiSupplierCatalogEditorModel=>true,
        UiPurchaseRequestEditorModel m=>m.Status==(byte)PurchaseRequestStatus.Draft,
        UiPurchaseOrderEditorModel m=>m.Status==(byte)PurchaseOrderStatus.Draft,
        UiPurchaseReceiptEditorModel m=>m.Status==(byte)PurchaseReceiptStatus.Draft,
        UiPurchaseInvoiceEditorModel m=>m.Status==(byte)PurchaseInvoiceStatus.Draft,
        _=>false
    };
    private static Guid? ModelId(object model)=>model switch{UiSupplierCatalogEditorModel x=>x.Id,UiPurchaseRequestEditorModel x=>x.Id,UiPurchaseOrderEditorModel x=>x.Id,UiPurchaseReceiptEditorModel x=>x.Id,UiPurchaseInvoiceEditorModel x=>x.Id,_=>null};
    private static string ModelTitle(object model)=>model switch{UiSupplierCatalogEditorModel x=>x.SupplierProductName??x.ProductName??"كتالوج المورد",UiPurchaseRequestEditorModel x=>x.RequestCode,UiPurchaseOrderEditorModel x=>$"{x.PurchaseOrderCode} - {x.SupplierName}",UiPurchaseReceiptEditorModel x=>x.ReceiptCode,UiPurchaseInvoiceEditorModel x=>x.PurchaseInvoiceCode,_=>"المشتريات"};
    private static string Route(PurchasingEntityType t)=>t switch{PurchasingEntityType.SupplierCatalog=>"/purchases/supplier-catalog",PurchasingEntityType.PurchaseRequests=>"/purchases/requests",PurchasingEntityType.PurchaseOrders=>"/purchases/orders",PurchasingEntityType.PurchaseReceipts=>"/purchases/receipts",PurchasingEntityType.PurchaseInvoices=>"/purchases/invoices",_=>"/purchases"};
    private static string Icon(PurchasingEntityType t)=>t switch{PurchasingEntityType.SupplierCatalog=>"fa-solid fa-box-open",PurchasingEntityType.PurchaseRequests=>"fa-solid fa-clipboard-list",PurchasingEntityType.PurchaseOrders=>"fa-solid fa-file-signature",PurchasingEntityType.PurchaseReceipts=>"fa-solid fa-truck-ramp-box",PurchasingEntityType.PurchaseInvoices=>"fa-solid fa-file-invoice-dollar",_=>"fa-solid fa-bag-shopping"};
    private static string ListTitle(PurchasingEntityType t)=>t switch{PurchasingEntityType.SupplierCatalog=>"كتالوج المورد",PurchasingEntityType.PurchaseRequests=>"طلبات الشراء",PurchasingEntityType.PurchaseOrders=>"أوامر الشراء",PurchasingEntityType.PurchaseReceipts=>"الاستلام",PurchasingEntityType.PurchaseInvoices=>"فواتير المشتريات",_=>"المشتريات"};
    private static string NewTitle(PurchasingEntityType t)=>t switch{PurchasingEntityType.SupplierCatalog=>"صنف مورد جديد",PurchasingEntityType.PurchaseRequests=>"طلب شراء جديد",PurchasingEntityType.PurchaseOrders=>"أمر شراء جديد",PurchasingEntityType.PurchaseReceipts=>"استلام جديد",PurchasingEntityType.PurchaseInvoices=>"فاتورة مشتريات جديدة",_=>"جديد"};
    private static string SortField(PurchasingEntityType t)=>t switch{PurchasingEntityType.SupplierCatalog=>"CreatedAt",PurchasingEntityType.PurchaseRequests=>"RequestDate",PurchasingEntityType.PurchaseOrders=>"OrderDate",PurchasingEntityType.PurchaseReceipts=>"ReceiptDate",PurchasingEntityType.PurchaseInvoices=>"InvoiceDate",_=>"CreatedAt"};
}
