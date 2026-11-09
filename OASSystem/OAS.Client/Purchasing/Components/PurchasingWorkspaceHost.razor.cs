using static OAS.UiLib.Core.Formatting.PurchasingDisplayFormat;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Accounting.Services;
using OAS.Client.Common.Feedback.Services;
using OAS.Client.Inventory.Services;
using OAS.Client.Purchasing.Common;
using OAS.Client.Printing.Services;
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
using OAS.UiLib.Components.Purchasing;
using OAS.UiLib.Core.Formatting;
using OAS.UiLib.Services.Dialogs;
using OAS.UiLib.Services.Feedback;

namespace OAS.Client.Purchasing.Components;

public partial class PurchasingWorkspaceHost
{
    private const int PageSize=24;
    [Parameter,EditorRequired] public PurchasingEntityType Section {get;set;}
    [Parameter] public Guid? InitialRecordId {get;set;}
    [Parameter] public Guid? RelatedPurchaseOrderId {get;set;}
    private Guid? _previousOrderFilter;
    private readonly Dictionary<Guid, PurchaseOrderProgressDto> _orderProgress = [];
    private PurchaseOrderProgressDto? ActiveOrderProgress
    {
        get
        {
            var orderId = ActiveTab?.Model switch
            {
                UiPurchaseOrderEditorModel order => order.Id,
                UiPurchaseReceiptEditorModel receipt => receipt.PurchaseOrderId,
                _ => null
            };
            return orderId.HasValue && _orderProgress.TryGetValue(orderId.Value, out var result) ? result : null;
        }
    }
    [Parameter] public Guid? InitialPurchaseOrderId {get;set;}
    private Guid? _lastReceiptFromOrderId;
    private (PurchasingEntityType Type, Guid Id)? _lastLinkedRecord;
    [Inject] private IPurchasingClientService Purchasing {get;set;}=default!;
    [Inject] private IInventoryClientService Inventory {get;set;}=default!;
    [Inject] private IAccountingClientService Accounting {get;set;}=default!;
    [Inject] private IPrintingClientService Printing {get;set;}=default!;
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
    private bool _isPrintingPurchaseInvoice;
    private IReadOnlyList<UiPurchasingListItem> CurrentListItems=[];
    private IReadOnlyList<UiPurchaseOrderCardItem> CurrentOrderCards=[];
    private IReadOnlyList<UiPurchaseInvoiceCardItem> CurrentInvoiceCards=[];
    private string _orderGroup = "all";
    private string _invoiceStatus = "all";
    private string _invoiceSort = "date_desc";
    private static (string Field, SortDirection Direction) InvoiceSort(string value) => value switch
    {
        "date_asc" => ("InvoiceDate", SortDirection.Ascending),
        "amount_desc" => ("TotalAmount", SortDirection.Descending),
        "amount_asc" => ("TotalAmount", SortDirection.Ascending),
        "supplier_asc" => ("SupplierName", SortDirection.Ascending),
        "supplier_desc" => ("SupplierName", SortDirection.Descending),
        _ => ("InvoiceDate", SortDirection.Descending)
    };
    private static PurchaseInvoiceStatus? SelectedInvoiceStatus(string selected) => selected switch
    {
        "draft" => PurchaseInvoiceStatus.Draft,
        "confirmed" => PurchaseInvoiceStatus.Confirmed,
        "matching" => PurchaseInvoiceStatus.PendingMatchApproval,
        "posted" => PurchaseInvoiceStatus.Posted,
        "cancelled" => PurchaseInvoiceStatus.Cancelled,
        _ => null
    };
    private static UiPurchaseInvoiceCardItem ToInvoiceCard(PurchaseInvoiceDto invoice)
        => new(invoice.Id, invoice.PurchaseInvoiceCode,
            invoice.SupplierName ?? invoice.SupplierCode ?? "مورد غير محدد",
            invoice.SupplierInvoiceCode, (byte)invoice.Status,
            PurchasingArabicPresenter.InvoiceStatus(invoice.Status),
            invoice.TotalAmount, invoice.CurrencyNameAr, invoice.CurrencyCode,
            invoice.InvoiceDate, invoice.DueDate, invoice.Lines.Count,
            invoice.SavedMatchingAllocationCount, invoice.UnresolvedMatchingAllocationCount);
    private static PurchaseOrderListGroup? SelectedOrderGroup(string selected) => selected switch
    {
        "approval" => PurchaseOrderListGroup.PendingApproval,
        "supply" => PurchaseOrderListGroup.InSupply,
        "completed" => PurchaseOrderListGroup.Completed,
        _ => null
    };
    private static UiPurchaseOrderCardItem ToOrderCard(PurchaseOrderDto order)
    {
        var completed = order.Lines.Count(x => x.BaseQuantity > 0m && x.RemainingBaseQuantity <= 0m);
        var started = order.Lines.Count(x => x.ReceivedBaseQuantity > 0m);
        var overdue = order.ExpectedDeliveryDate is { } due && due < DateOnly.FromDateTime(DateTime.Today)
            && (order.Status is PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartiallyReceived);
        return new UiPurchaseOrderCardItem(order.Id, order.PurchaseOrderCode,
            order.SupplierName ?? order.SupplierCode ?? "مورد غير محدد",
            order.DestinationWarehouseName ?? order.DestinationWarehouseCode,
            PurchasingArabicPresenter.OrderStatus(order.Status), (byte)order.Status,
            order.TotalAmount, order.CurrencyCode, order.OrderDate, order.ExpectedDeliveryDate,
            completed, started, order.Lines.Count, overdue, order.CurrencyNameAr);
    }
    private readonly Dictionary<Guid,PurchaseInvoicePaymentSummaryDto> _invoicePaymentSummaries=[];
    private readonly HashSet<Guid> _loadingInvoicePaymentSummaries=[];

    private UiPurchaseOrderEditor? _purchaseOrderEditor;
    private UiPurchaseReceiptEditor? _purchaseReceiptEditor;
    private PurchasingTabState? ActiveTab=>Workspace.Find(Workspace.ActiveTabId);
    private PurchaseInvoicePaymentSummaryDto? ActiveInvoicePaymentSummary
        => ActiveTab?.Model is UiPurchaseInvoiceEditorModel { Id: Guid id } && _invoicePaymentSummaries.TryGetValue(id,out var summary) ? summary : null;
    private bool IsActiveInvoicePaymentSummaryLoading
        => ActiveTab?.Model is UiPurchaseInvoiceEditorModel { Id: Guid id } && _loadingInvoicePaymentSummaries.Contains(id);
    private bool ShowInvoicePaymentSummary
        => ActiveTab?.Model is UiPurchaseInvoiceEditorModel m && m.Status==(byte)PurchaseInvoiceStatus.Posted && m.Id.HasValue;
    private string ActiveInvoicePaymentStatusText=>PaymentStatusText(ActiveInvoicePaymentSummary?.PaymentStatus);
    private string PayInvoiceButtonText
        => ActiveInvoicePaymentSummary is { OutstandingAmount: <= 0m } ? "مسددة بالكامل"
            : ActiveInvoicePaymentSummary is { PaidAmount: > 0m } ? "سداد المتبقي"
            : "سداد المورد";
    private bool PayInvoiceDisabled=>IsActiveInvoicePaymentSummaryLoading || ActiveInvoicePaymentSummary is { OutstandingAmount: <= 0m };
    private bool IsListTab=>ActiveTab is null || ActiveTab.IsListTab;
    private bool IsEditable=>ActiveTab is { } t && !t.IsListTab && (t.IsNew||t.IsEditMode) && IsDomainEditable(t.Model);
    private string SaveButtonText => ActiveTab?.Model is UiPurchaseInvoiceEditorModel { Id: null, SourceReceiptId: not null }
        ? "حفظ ومطابقة تلقائية"
        : "حفظ";
    private bool CanBeginEdit=>ActiveTab is {IsListTab:false,IsNew:false,IsEditMode:false} t && IsDomainEditable(t.Model);
    private bool CanSave=>ActiveTab is {IsListTab:false} t && (t.IsNew||t.IsEditMode) && IsDomainEditable(t.Model);
    private bool CanCancelEdit=>ActiveTab is {IsListTab:false} t && (t.IsNew||t.IsEditMode);
    private string TabsAriaLabel=>$"تبويبات {ListTitle(Section)}";
    private string NewButtonText=>Section switch
    {
        PurchasingEntityType.SupplierCatalog=>"صنف مورد جديد",
        PurchasingEntityType.PurchaseRequests=>"طلب شراء جديد",
        PurchasingEntityType.PurchaseOrders=>"أمر شراء جديد",
        PurchasingEntityType.PurchaseReceipts=>"استلام جديد",
        PurchasingEntityType.PurchaseInvoices=>"فاتورة مورد جديدة",
        _=>"جديد"
    };
    private string SearchPlaceholder=>Section switch
    {
        PurchasingEntityType.SupplierCatalog=>"ابحث بالمورد أو المنتج أو كود المورد...",
        PurchasingEntityType.PurchaseRequests=>"ابحث برقم الطلب أو المخزن أو السبب...",
        PurchasingEntityType.PurchaseOrders=>"ابحث برقم الأمر أو المورد أو المخزن...",
        PurchasingEntityType.PurchaseReceipts=>"ابحث برقم الاستلام أو المورد أو أمر الشراء...",
        PurchasingEntityType.PurchaseInvoices=>"ابحث برقم الفاتورة أو المورد أو رقم فاتورة المورد...",
        _=>"بحث..."
    };

    private IReadOnlyList<UiApplicationTabItem> WorkspaceTabs=>Workspace.VisibleTabs(Section)
        .Select(t=>new UiApplicationTabItem(t.TabId,t.Title,Route(Section),Icon(Section),Workspace.ActiveTabId==t.TabId,t.CanClose)).ToArray();

    protected override async Task OnParametersSetAsync()
    {
        if(_initializedSection!=Section)
        {
            _initializedSection=Section;
            _lastLinkedRecord=null;
            var activeId=Workspace.ActiveTab?.EntityType==Section?Workspace.ActiveTabId:Guid.Empty;
            Workspace.OpenList(Section);
            if(activeId!=Guid.Empty)Workspace.ActiveTabId=activeId;
            _search=null;_pageNumber=1;_orderGroup="all";CurrentOrderCards=[];_invoiceStatus="all";_invoiceSort="date_desc";CurrentInvoiceCards=[];
            if(IsListTab)await LoadListAsync(); else if(ActiveTab is { } tab)await EnsureLoadedAsync(tab);
        }
        if(Section == PurchasingEntityType.PurchaseReceipts && _previousOrderFilter != RelatedPurchaseOrderId)
        {
            _previousOrderFilter = RelatedPurchaseOrderId;
            _pageNumber = 1;
            if(IsListTab) await LoadListAsync();
        }
        if(InitialRecordId.HasValue && _lastLinkedRecord != (Section,InitialRecordId.Value))
        {
            _lastLinkedRecord = (Section,InitialRecordId.Value);
            await OpenRecordAsync(InitialRecordId.Value);
        }
        if(Section == PurchasingEntityType.PurchaseReceipts && InitialPurchaseOrderId.HasValue &&
           _lastReceiptFromOrderId != InitialPurchaseOrderId)
        {
            _lastReceiptFromOrderId = InitialPurchaseOrderId;
            var tab = Workspace.OpenNew(Section, "استلام من أمر شراء");
            tab.Model = new UiPurchaseReceiptEditorModel();
            tab.IsEditMode = true;
            await ReceiptPurchaseOrderChangedAsync(InitialPurchaseOrderId.Value);
        }
    }

    private async Task LoadListAsync()
    {
        if(_isLoadingList)return;_isLoadingList=true;
        try
        {
            var invoiceSort = InvoiceSort(_invoiceSort);
            var request=new PageRequest{PageNumber=_pageNumber,PageSize=PageSize,Search=string.IsNullOrWhiteSpace(_search)?null:_search.Trim(),
                SortBy=Section==PurchasingEntityType.PurchaseInvoices?invoiceSort.Field:SortField(Section),
                SortDirection=Section==PurchasingEntityType.PurchaseInvoices?invoiceSort.Direction:SortDirection.Descending};
            switch(Section)
            {
                case PurchasingEntityType.SupplierCatalog:
                    var c=await Purchasing.GetSupplierCatalogAsync(request);SetPage(c.PageNumber,c.TotalPages,c.TotalCount,c.Items.Select(x=>new UiPurchasingListItem(x.Id,x.SupplierProductCode??x.ProductCode??"—",x.ProductName??x.SupplierProductName??"صنف",x.SupplierName,x.IsActive?"فعال":"غير فعال",x.PurchaseUnitName,"fa-solid fa-box-open")).ToArray());break;
                case PurchasingEntityType.PurchaseRequests:
                    var r=await Purchasing.GetPurchaseRequestsAsync(request);SetPage(r.PageNumber,r.TotalPages,r.TotalCount,r.Items.Select(x=>new UiPurchasingListItem(x.Id,x.RequestCode,x.Reason??x.WarehouseName??"طلب شراء",x.WarehouseName,PurchasingArabicPresenter.RequestStatus(x.Status),DisplayDate(x.RequestDate),"fa-solid fa-clipboard-list")).ToArray());break;
                case PurchasingEntityType.PurchaseOrders:
                    var o=await Purchasing.GetPurchaseOrdersAsync(request,group:SelectedOrderGroup(_orderGroup));
                    CurrentOrderCards=o.Items.Select(ToOrderCard).ToArray();
                    SetPage(o.PageNumber,o.TotalPages,o.TotalCount,[]);break;
                case PurchasingEntityType.PurchaseReceipts:
                    var gr=await Purchasing.GetPurchaseReceiptsAsync(request, purchaseOrderId: RelatedPurchaseOrderId);SetPage(gr.PageNumber,gr.TotalPages,gr.TotalCount,gr.Items.Select(x=>new UiPurchasingListItem(x.Id,x.ReceiptCode,x.SupplierName??"استلام",x.PurchaseOrderCode,PurchasingArabicPresenter.ReceiptStatus(x.Status),DisplayDate(x.ReceiptDate),"fa-solid fa-truck-ramp-box")).ToArray());break;
                case PurchasingEntityType.PurchaseInvoices:
                    var pi=await Purchasing.GetPurchaseInvoicesAsync(request,status:SelectedInvoiceStatus(_invoiceStatus));
                    CurrentInvoiceCards=pi.Items.Select(ToInvoiceCard).ToArray();
                    SetPage(pi.PageNumber,pi.TotalPages,pi.TotalCount,[]);break;
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
    private static UiPurchaseInvoiceEditorModel NewInvoice()=>new();

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
            if(tab.Model is UiPurchaseInvoiceEditorModel invoice)
            {
                await LoadInvoicePaymentSummaryAsync(invoice,force);
                await LoadInvoiceMatchingReadinessAsync(invoice);
            }
            if(tab.Model is UiPurchaseOrderEditorModel order && order.Id is Guid orderId)
                await LoadOrderProgressAsync(orderId);
            if(tab.Model is UiPurchaseReceiptEditorModel receipt && receipt.PurchaseOrderId is Guid purchaseOrderId)
                await LoadOrderProgressAsync(purchaseOrderId);
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);Workspace.Close(tab.TabId);}catch{ApiFeedback.ShowUnexpected();Workspace.Close(tab.TabId);}
        finally{tab.IsLoading=false;}
    }

    private async Task LoadInvoiceMatchingReadinessAsync(UiPurchaseInvoiceEditorModel invoice)
    {
        invoice.MatchingNotice = null;
        if (invoice.Id is not Guid id ||
            (invoice.Status != (byte)PurchaseInvoiceStatus.Draft &&
             invoice.Status != (byte)PurchaseInvoiceStatus.Confirmed &&
             invoice.Status != (byte)PurchaseInvoiceStatus.PendingMatchApproval))
            return;
        try
        {
            var readiness = await Purchasing.GetPurchaseInvoiceMatchingReadinessAsync(id);
            if (readiness is { IsComplete: false }) invoice.MatchingNotice = readiness.Message;
            // Rehydrate saved matching allocations when reopening any editable invoice.
            // Otherwise the "Approve variances" action disappears after a refresh.
            if (readiness is { AllocationCount: > 0 } && invoice.Match is null)
            {
                var match = new UiPurchaseMatchModel
                {
                    OverallStatus = readiness.IsComplete ? "المطابقة مكتملة" : "تحتاج مراجعة",
                    RequiresApproval = readiness.Allocations.Any(x =>
                        x.MatchStatus == PurchaseMatchStatus.RequiresApproval)
                };
                match.Allocations.AddRange(readiness.Allocations.Select(x =>
                    new UiPurchaseMatchAllocationModel
                    {
                        Id = x.Id,
                        PurchaseInvoiceLineId = x.PurchaseInvoiceLineId,
                        PurchaseReceiptLineId = x.PurchaseReceiptLineId,
                        MatchedQuantity = x.MatchedQuantity,
                        MatchedNetAmount = x.MatchedNetAmount,
                        QuantityVariance = x.QuantityVariance,
                        PriceVarianceAmount = x.PriceVarianceAmount,
                        TaxVarianceAmount = x.TaxVarianceAmount,
                        Status = PurchasingArabicPresenter.MatchStatus(x.MatchStatus),
                        RowVersion = x.RowVersion
                    }));
                invoice.Match = match;
            }
        }
        catch (ApiClientException)
        {
            // Do not block viewing the invoice when the optional readiness check fails.
            // The explicit posting action always repeats this check and reports its error.
        }
    }

    private async Task LoadOrderProgressAsync(Guid orderId)
    {
        try
        {
            var progress = await Purchasing.GetPurchaseOrderProgressAsync(orderId);
            if (progress is not null) _orderProgress[orderId] = progress;
        }
        catch (ApiClientException)
        {
            // The progress endpoint requires separate invoices/receipts permissions.
            // Do not prevent permitted order/receipt access when a role lacks them.
            _orderProgress.Remove(orderId);
        }
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
    private async Task SelectTabAsync(Guid id){Workspace.ActiveTabId=id;if(Workspace.Find(id) is { } t){await EnsureLoadedAsync(t);if(t.Model is UiPurchaseInvoiceEditorModel invoice)
        {
            await LoadInvoicePaymentSummaryAsync(invoice);
            await LoadInvoiceMatchingReadinessAsync(invoice);
        }}if(IsListTab)await LoadListAsync();}
    private async Task CloseTabAsync(Guid id){var tab=Workspace.Find(id);if(tab is null||!tab.CanClose)return;if(!await ConfirmDiscardAsync(tab))return;Workspace.Close(id);await InvokeAsync(StateHasChanged);}
    private async Task CloseOtherTabsAsync(Guid id){var keep=Workspace.Find(id);foreach(var tab in Workspace.VisibleTabs(Section).Where(x=>x.CanClose&&x.TabId!=id).ToArray()){if(!await ConfirmDiscardAsync(tab))return;}foreach(var tab in Workspace.VisibleTabs(Section).Where(x=>x.CanClose&&x.TabId!=id).ToArray())Workspace.Close(tab.TabId);Workspace.ActiveTabId=keep?.TabId??Workspace.OpenList(Section).TabId;}
    private async Task CloseAllTabsAsync(){var tabs=Workspace.VisibleTabs(Section).Where(x=>x.CanClose).ToArray();foreach(var t in tabs)if(!await ConfirmDiscardAsync(t))return;foreach(var t in tabs)Workspace.Close(t.TabId);Workspace.OpenList(Section);}
    private async Task<bool> ConfirmDiscardAsync(PurchasingTabState tab)=>!tab.IsDirty||await Dialog.ConfirmAsync("تغييرات غير محفوظة",$"توجد تغييرات غير محفوظة في «{tab.Title}». هل تريد تجاهلها؟",AlertTone.Warning,"تجاهل وإغلاق","رجوع");

    private async Task SaveAsync(MouseEventArgs _)
    {
        var tab=ActiveTab;
        if(tab is null || tab.IsSaving) return;
        if(tab.Model is UiPurchaseOrderEditorModel && _purchaseOrderEditor is not null &&
            ReferenceEquals(_purchaseOrderEditor.Model, tab.Model) && !_purchaseOrderEditor.ValidateForSave())
        {
            Snackbar.Warning("راجع الحقول المحددة بالأحمر: ستجد شرح الخطأ وطريقة التصحيح تحت كل حقل.");
            return;
        }
        if(tab.Model is UiPurchaseReceiptEditorModel && _purchaseReceiptEditor is not null &&
            ReferenceEquals(_purchaseReceiptEditor.Model, tab.Model) && !_purchaseReceiptEditor.ValidateForSave())
        {
            Snackbar.Warning("راجع الحقول المحددة بالأحمر في سند الاستلام، ثم أعد الحفظ.");
            return;
        }
        tab.IsSaving=true;
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
            tab.Model=saved;tab.EntityId=ModelId(saved);tab.Title=ModelTitle(saved);tab.IsEditMode=false;tab.IsDirty=false;
            if(saved is UiPurchaseInvoiceEditorModel invoiceWithError &&
                invoiceWithError.Lines.Any(x=>!string.IsNullOrWhiteSpace(x.QuantityError)))
                Snackbar.Warning("حُفظت الفاتورة كمسودة، لكن المطابقة تحتاج تصحيح الكمية المحددة بالأحمر.");
            else Snackbar.Success("تم حفظ البيانات بنجاح.");
        }
        catch(ApiClientException ex)
        {
            if (tab.Model is UiPurchaseOrderEditorModel)
                _purchaseOrderEditor?.ShowSaveError("لم يقبل الخادم حفظ أمر الشراء. راجع الحقول والرسالة التي يعرضها النظام، ثم أعد المحاولة.", ex.Error.Errors);
            if (tab.Model is UiPurchaseReceiptEditorModel &&
                string.Equals(ex.Error.Code, "validation_failed", StringComparison.OrdinalIgnoreCase))
            {
                _purchaseReceiptEditor?.ShowSaveError("رفض الخادم حفظ سند الاستلام. راجع الكميات والحقول المحددة ثم أعد المحاولة.", ex.Error.Errors);
                Snackbar.Warning("لم يُحفظ سند الاستلام. راجع رسائل الأخطاء أسفل الحقول.");
            }
            else ApiFeedback.Show(ex.Error);
        }
        catch
        {
            if (tab.Model is UiPurchaseReceiptEditorModel)
                _purchaseReceiptEditor?.ShowSaveError("حدث خطأ غير متوقع أثناء حفظ سند الاستلام. بياناتك ما زالت في النموذج؛ تحقق من الاتصال ثم أعد المحاولة.");
            if (tab.Model is UiPurchaseOrderEditorModel)
                _purchaseOrderEditor?.ShowSaveError("حدث خطأ غير متوقع عند الحفظ. تأكد من الاتصال ثم أعد المحاولة؛ بقيت بياناتك في النموذج.");
            ApiFeedback.ShowUnexpected();
        }
        finally{tab.IsSaving=false;}
    }

    private async Task<object?> SaveCatalogAsync(UiSupplierCatalogEditorModel m)
    {
        if(m.SupplierId is not Guid supplier||m.ProductVariantId is not Guid product||m.PurchaseUnitId is not Guid unit){Snackbar.Warning("حدد المورد والمنتج ووحدة الشراء.");return null;}
        if(QuantityInputRules.UsesWholeNumbers(null, m.PurchaseUnitName, m.Id.HasValue ? m.MinimumOrderQuantity : 0m) &&
            !QuantityInputRules.IsWhole(m.MinimumOrderQuantity))
        {
            Snackbar.Warning("الحد الأدنى للطلب لهذه الوحدة يجب أن يكون عددًا صحيحًا دون كسور.");
            return null;
        }
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
            var lines=UiPurchaseReceiptFormValidator.LinesToSubmit(m).Select(x=>new CreatePurchaseReceiptLineRequest(x.PurchaseOrderLineId,x.LineSequence,x.ReceivedQuantity,x.AcceptedQuantity,x.RejectedQuantity,x.ActualUnitCost,x.ExpiryDate,x.BatchCode,x.Notes)).ToArray();
            return Map((await Purchasing.CreatePurchaseReceiptAsync(new(po,m.ReceiptDate,m.PostingDate,m.SupplierDeliveryCode,m.Notes,lines)))!);
        }
        var updates=UiPurchaseReceiptFormValidator.LinesToSubmit(m).Select(x=>new UpdatePurchaseReceiptLineRequest(x.Id,x.PurchaseOrderLineId,x.LineSequence,x.ReceivedQuantity,x.AcceptedQuantity,x.RejectedQuantity,x.ActualUnitCost,x.ExpiryDate,x.BatchCode,x.Notes,x.RowVersion)).ToArray();
        return Map((await Purchasing.UpdatePurchaseReceiptAsync(m.Id.Value,new(m.ReceiptDate,m.PostingDate,m.SupplierDeliveryCode,m.Notes,updates,m.RowVersion??string.Empty)))!);
    }
    private static void ClearInvoiceQuantityErrors(UiPurchaseInvoiceEditorModel invoice)
    {
        invoice.SourceReceiptError=null;
        foreach (var line in invoice.Lines) line.QuantityError=null;
    }

    private static void ShowInvoiceQuantityIssue(UiPurchaseInvoiceEditorModel invoice,
        InvoiceReceiptAllocationIssue? issue, string fallback)
    {
        if(issue is null)
        {
            invoice.SourceReceiptError=fallback;
            return;
        }
        var line=invoice.Lines.FirstOrDefault(x=>x.LineSequence==issue.LineSequence);
        if(line is not null) line.QuantityError=issue.Message;
        else invoice.SourceReceiptError=fallback;
    }

    /// <summary>Recheck the same receipt after a server-side matching conflict, so
    /// the user sees a field-level error instead of an unhelpful global exception.</summary>
    private async Task<bool> RefreshInvoiceQuantityIssueAsync(UiPurchaseInvoiceEditorModel invoice,
        Guid receiptId, Guid? invoiceId)
    {
        try
        {
            var receipt=await Purchasing.GetPurchaseReceiptAsync(receiptId);
            if(receipt is null)return false;
            var availability=invoiceId.HasValue
                ? await Purchasing.GetPurchaseInvoiceMatchAvailabilityAsync(invoiceId.Value,
                    receipt.Lines.Select(x=>x.Id).ToArray())
                : await Purchasing.GetPurchaseReceiptInvoiceAvailabilityAsync(receiptId);
            var remaining=(availability??[]).ToDictionary(x=>x.ReceiptLineId,x=>x.AvailableQuantity);
            foreach(var line in invoice.Lines.Where(x=>x.SourceReceiptLineId.HasValue))
                line.AvailableToInvoiceQuantity=remaining.GetValueOrDefault(line.SourceReceiptLineId!.Value);
            var lines=AvailableReceiptLines(receipt)
                .Where(x=>remaining.ContainsKey(x.ReceiptLineId))
                .Select(x=>x with { AcceptedQuantity=remaining[x.ReceiptLineId], ReturnedQuantity=0m })
                .ToArray();
            var demands=invoice.Lines.Select(x=>new InvoiceReceiptDemand(x.Id??Guid.Empty,
                x.PurchaseOrderLineId,x.ProductVariantId,x.Quantity,x.LineSequence,
                x.SourceReceiptLineId)).ToArray();
            if(!PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(demands,lines,
                out _,out var issue,out var error))
            {
                ShowInvoiceQuantityIssue(invoice,issue,error);
                return true;
            }
        }
        catch(Exception)
        {
            // This optional UI diagnostic must never mask the original server rejection.
        }
        return false;
    }

    private static IReadOnlyList<ReceiptAvailableLine> AvailableReceiptLines(PurchaseReceiptDto receipt)
        => receipt.Lines.Select(x=>new ReceiptAvailableLine(x.Id,x.PurchaseOrderLineId,
            x.ProductVariantId,x.AcceptedQuantity,x.ReturnedQuantity)).ToArray();

    private static void PreserveInvoiceReceiptLineContext(
        UiPurchaseInvoiceEditorModel source, UiPurchaseInvoiceEditorModel target)
    {
        // A fresh API DTO does not carry the UI-only selected receipt-line identity.
        // Preserve it for error messages and for an automatic retry in the same tab.
        var originals=source.Lines.ToDictionary(x=>x.LineSequence);
        foreach(var line in target.Lines)
        {
            if(!originals.TryGetValue(line.LineSequence,out var original))continue;
            line.SourceReceiptLineId=original.SourceReceiptLineId;
            line.SourceReceiptLineSequence=original.SourceReceiptLineSequence;
            line.AvailableToInvoiceQuantity=original.AvailableToInvoiceQuantity;
            line.ProductCode??=original.ProductCode;
        }
    }

    private async Task<object?> SaveInvoiceAsync(UiPurchaseInvoiceEditorModel m)
    {
        ClearInvoiceQuantityErrors(m);
        if(m.SupplierId is not Guid supplier||m.CurrencyId is not Guid currency||m.Lines.Count==0||m.Lines.Any(x=>!x.ProductVariantId.HasValue))
        {
            m.SourceReceiptError="اختر استلامًا مرحّلًا يحتوي أصنافًا صحيحة وكميات متاحة للمطابقة.";
            return null;
        }
        if(m.Lines.Any(x=>!x.PurchaseOrderLineId.HasValue))
        {
            m.SourceReceiptError="يجب أن ترتبط أصناف الفاتورة بأمر شراء. اختر استلامًا مرحّلًا أولًا.";
            return null;
        }
        if(m.ExchangeRate<=0m){Snackbar.Warning("سعر الصرف يجب أن يكون أكبر من صفر.");return null;}
        var invalidQuantity=m.Lines.FirstOrDefault(x=>x.Quantity<=0m);
        if(invalidQuantity is not null)
        {
            invalidQuantity.QuantityError="أدخل كمية أكبر من صفر لهذا الصنف.";
            return null;
        }

        PurchaseReceiptDto? sourceReceipt=null;
        if(!m.Id.HasValue)
        {
            if(m.SourceReceiptId is not Guid sourceId)
            {
                m.SourceReceiptError="اختر سند استلام مرحّلًا لتعبئة الفاتورة تلقائيًا.";
                return null;
            }
            sourceReceipt=await Purchasing.GetPurchaseReceiptAsync(sourceId);
            if(sourceReceipt is null || sourceReceipt.Status!=PurchaseReceiptStatus.Posted || sourceReceipt.SupplierId!=supplier)
            {
                m.SourceReceiptError="سند الاستلام غير مرحّل أو المورد لا يطابق الفاتورة. اختر استلامًا صالحًا.";
                return null;
            }
            // The receipt's AcceptedQuantity does not deduct quantities already invoiced.
            // Check the true ledger availability BEFORE creating any invoice draft.
            var receiptAvailability=await Purchasing.GetPurchaseReceiptInvoiceAvailabilityAsync(sourceId);
            var remainingBeforeCreate=(receiptAvailability??[]).ToDictionary(x=>x.ReceiptLineId,x=>x.AvailableQuantity);
            foreach(var line in m.Lines.Where(x=>x.SourceReceiptLineId.HasValue))
                line.AvailableToInvoiceQuantity=remainingBeforeCreate.GetValueOrDefault(line.SourceReceiptLineId!.Value);
            var currentCandidates=AvailableReceiptLines(sourceReceipt)
                .Where(x=>remainingBeforeCreate.ContainsKey(x.ReceiptLineId))
                .Select(x=>x with { AcceptedQuantity=remainingBeforeCreate[x.ReceiptLineId],ReturnedQuantity=0m })
                .ToArray();
            var draftDemands=m.Lines.Select(x=>new InvoiceReceiptDemand(Guid.Empty,x.PurchaseOrderLineId,
                x.ProductVariantId,x.Quantity,x.LineSequence,x.SourceReceiptLineId)).ToArray();
            if(!PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(draftDemands,currentCandidates,
                out _,out var quantityIssue,out var preflightError))
            {
                ShowInvoiceQuantityIssue(m,quantityIssue,preflightError);
                return null;
            }

            var lines=m.Lines.Select(x=>new CreatePurchaseInvoiceLineRequest(x.LineSequence,x.PurchaseOrderLineId,
                x.ProductVariantId!.Value,x.Quantity,x.UnitPrice,x.DiscountAmount,x.TaxRate)).ToArray();
            var created=await Purchasing.CreatePurchaseInvoiceAsync(new(m.SupplierInvoiceCode,supplier,
                m.InvoiceDate,m.PostingDate,currency,m.ExchangeRate,m.ExchangeRateDate,
                (TaxCalculationMode)m.TaxCalculationMode,m.Notes,lines));
            if(created is null)return null;

            var mapped=Map(created);
            PreserveInvoiceReceiptLineContext(m,mapped);
            mapped.SourceReceiptId=sourceId;
            mapped.SourceReceiptCode=sourceReceipt.ReceiptCode;
            // Creating the invoice and matching it are separate calls. A failed match must
            // never look like an entirely failed save or trigger a duplicate invoice creation.
            try
            {
                var originalReceiptLines=m.Lines.ToDictionary(x=>x.LineSequence,
                    x=>x.SourceReceiptLineId);
                var demands=created.Lines.Select(x=>new InvoiceReceiptDemand(x.Id,x.PurchaseOrderLineId,
                    x.ProductVariantId,x.Quantity,x.LineSequence,
                    originalReceiptLines.GetValueOrDefault(x.LineSequence))).ToArray();
                // Compute remaining quantities using the server's matching ledger, not only
                // AcceptedQuantity - ReturnedQuantity from the receipt document.
                var availability=await Purchasing.GetPurchaseInvoiceMatchAvailabilityAsync(created.Id,
                    sourceReceipt.Lines.Select(x=>x.Id).ToArray());
                var remaining=(availability??[]).ToDictionary(x=>x.ReceiptLineId,x=>x.AvailableQuantity);
                var availableLines=AvailableReceiptLines(sourceReceipt)
                    .Where(x=>remaining.ContainsKey(x.ReceiptLineId))
                    .Select(x=>x with{AcceptedQuantity=remaining[x.ReceiptLineId],ReturnedQuantity=0m})
                    .ToArray();
                if(!PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(demands,availableLines,
                    out var plan,out var matchIssue,out var planError))
                {
                    ShowInvoiceQuantityIssue(mapped,matchIssue,planError);
                    mapped.MatchingNotice="تم حفظ الفاتورة كمسودة، لكن تعذرت المطابقة. " +
                        "راجع الكمية المحددة بالأحمر ثم أعد المطابقة؛ لا تنشئ فاتورة ثانية.";
                    Snackbar.Warning("حُفظت المسودة. راجع حقل الكمية المحدد بالأحمر.");
                    return mapped;
                }
                var requests=plan.Select(x=>new PurchaseInvoiceMatchAllocationRequest(
                    x.InvoiceLineId,x.ReceiptLineId,x.Quantity)).ToArray();
                var match=await Purchasing.MatchPurchaseInvoiceAsync(created.Id,new(requests,created.RowVersion));
                var refreshed=await Purchasing.GetPurchaseInvoiceAsync(created.Id);
                if(refreshed is not null)
                {
                    mapped=Map(refreshed);
                    PreserveInvoiceReceiptLineContext(m,mapped);
                }
                mapped.SourceReceiptId=sourceId;
                mapped.SourceReceiptCode=sourceReceipt.ReceiptCode;
                if(match is not null)
                {
                    mapped.Match=Map(match);
                    mapped.MatchingNotice=match.RequiresApproval
                        ? "تمت المطابقة تلقائيًا، لكن توجد فروقات تحتاج إلى المراجعة والاعتماد."
                        : "تمت المطابقة تلقائيًا مع الاستلام. راجع الفاتورة ثم اضغط تأكيد قبل الترحيل.";
                }
                else mapped.MatchingNotice="حُفظت المسودة. راجع المطابقة قبل التأكيد والترحيل.";
            }
            catch(ApiClientException ex)
            {
                var isQuantityConflict=ex.Error.Code is
                    "purchasing_match_quantity_exceeds_available_receipt" or
                    "purchasing_match_quantity_exceeds_invoice";
                var fieldMarked=isQuantityConflict &&
                    await RefreshInvoiceQuantityIssueAsync(mapped,sourceId,created.Id);
                mapped.MatchingNotice=fieldMarked
                    ? "حُفظت الفاتورة كمسودة، لكن المطابقة لم تكتمل. راجع الكمية المحددة بالأحمر؛ لا تنشئ فاتورة أخرى."
                    : "حُفظت الفاتورة كمسودة، لكن المطابقة التلقائية لم تكتمل: " +
                      PurchaseInvoiceMatchingMessages.FromApiError(ex.Error) +
                      " لا تنشئ فاتورة أخرى لنفس الاستلام.";
                Snackbar.Warning(fieldMarked
                    ? "حُفظت المسودة. راجع رسالة الكمية أسفل الصنف المحدد."
                    : mapped.MatchingNotice);
            }
            catch(Exception)
            {
                // Never leave a successfully created invoice looking unsaved: doing so
                // encourages the user to create the same supplier invoice twice.
                mapped.MatchingNotice="تم حفظ الفاتورة كمسودة، لكن تعذر التأكد من المطابقة التلقائية. حدّث هذه الفاتورة وراجع المطابقة قبل المتابعة؛ لا تنشئ فاتورة ثانية.";
                Snackbar.Warning(mapped.MatchingNotice);
            }
            return mapped;
        }

        var updates=m.Lines.Select(x=>new UpdatePurchaseInvoiceLineRequest(x.Id,x.LineSequence,
            x.PurchaseOrderLineId,x.ProductVariantId!.Value,x.Quantity,x.UnitPrice,
            x.DiscountAmount,x.TaxRate,x.RowVersion)).ToArray();
        return Map((await Purchasing.UpdatePurchaseInvoiceAsync(m.Id.Value,new(m.SupplierInvoiceCode,supplier,
            m.InvoiceDate,m.PostingDate,currency,m.ExchangeRate,m.ExchangeRateDate,
            (TaxCalculationMode)m.TaxCalculationMode,m.Notes,updates,m.RowVersion??string.Empty)))!);
    }

    private async Task LoadInvoicePaymentSummaryAsync(UiPurchaseInvoiceEditorModel invoice,bool force=false)
    {
        if(invoice.Id is not Guid id)return;
        if(invoice.Status!=(byte)PurchaseInvoiceStatus.Posted)
        {
            _invoicePaymentSummaries.Remove(id);
            return;
        }
        if(!force&&_invoicePaymentSummaries.ContainsKey(id))return;
        if(!_loadingInvoicePaymentSummaries.Add(id))return;
        try
        {
            var summary=await Purchasing.GetPurchaseInvoicePaymentSummaryAsync(id);
            if(summary is not null)_invoicePaymentSummaries[id]=summary;
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}
        catch{ApiFeedback.ShowUnexpected();}
        finally
        {
            _loadingInvoicePaymentSummaries.Remove(id);
            await InvokeAsync(StateHasChanged);
        }
    }

    private static string PaymentStatusText(string? status)=>status switch
    {
        "Paid"=>"مسددة بالكامل",
        "PartiallyPaid"=>"مسددة جزئياً",
        "Unpaid"=>"غير مسددة",
        "NotPosted"=>"غير مرحلة",
        _=>"غير محددة"
    };

    private async Task ChangeOrderGroupAsync(string group)
    {
        if (_isLoadingList || _orderGroup == group ||
            group is not ("all" or "approval" or "supply" or "completed")) return;
        _orderGroup = group;
        _pageNumber = 1;
        await LoadListAsync();
    }
    private async Task ChangeInvoiceStatusAsync(string status)
    {
        if (_isLoadingList || _invoiceStatus == status ||
            status is not ("all" or "draft" or "confirmed" or "matching" or "posted" or "cancelled")) return;
        _invoiceStatus = status;
        _pageNumber = 1;
        await LoadListAsync();
    }
    private async Task ChangeInvoiceSortAsync(string value)
    {
        if (_isLoadingList || _invoiceSort == value ||
            value is not ("date_desc" or "date_asc" or "amount_desc" or "amount_asc" or "supplier_asc" or "supplier_desc")) return;
        _invoiceSort = value;
        _pageNumber = 1;
        await LoadListAsync();
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
