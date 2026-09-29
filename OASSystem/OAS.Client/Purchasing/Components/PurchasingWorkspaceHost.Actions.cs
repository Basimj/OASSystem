using OAS.Client.Services.Http;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Purchasing.Workspace;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Contracts.Purchasing.PurchaseRequests;
using OAS.Contracts.Sales.Enums;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models.Purchasing;

namespace OAS.Client.Purchasing.Components;

public partial class PurchasingWorkspaceHost
{
    private bool ShowSubmitRequest=>ActiveTab?.Model is UiPurchaseRequestEditorModel m&&m.Status==(byte)PurchaseRequestStatus.Draft&&!ActiveTab.IsDirty;
    private bool ShowApproveRequest=>ActiveTab?.Model is UiPurchaseRequestEditorModel m&&m.Status==(byte)PurchaseRequestStatus.PendingApproval;
    private bool ShowCancelRequest=>ActiveTab?.Model is UiPurchaseRequestEditorModel m&&(m.Status is (byte)PurchaseRequestStatus.Draft or (byte)PurchaseRequestStatus.Approved);
    private bool ShowCreateOrderFromRequest=>ActiveTab?.Model is UiPurchaseRequestEditorModel m&&(m.Status is (byte)PurchaseRequestStatus.Approved or (byte)PurchaseRequestStatus.PartiallyConverted);
    private bool ShowSubmitOrder=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&m.Status==(byte)PurchaseOrderStatus.Draft&&!ActiveTab.IsDirty;
    private bool ShowApproveOrder=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&m.Status==(byte)PurchaseOrderStatus.PendingApproval;
    private bool ShowSendOrder=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&m.Status==(byte)PurchaseOrderStatus.Approved;
    private bool ShowCreateReceipt=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&(m.Status is (byte)PurchaseOrderStatus.Sent or (byte)PurchaseOrderStatus.PartiallyReceived);
    private bool ShowCloseOrder=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&m.Status==(byte)PurchaseOrderStatus.FullyReceived;
    private bool ShowCancelOrder=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&(m.Status is (byte)PurchaseOrderStatus.Draft or (byte)PurchaseOrderStatus.Approved or (byte)PurchaseOrderStatus.Sent);
    private bool ShowConfirmReceipt=>ActiveTab?.Model is UiPurchaseReceiptEditorModel m&&m.Status==(byte)PurchaseReceiptStatus.Draft&&!ActiveTab.IsDirty;
    private bool ShowPostReceipt=>ActiveTab?.Model is UiPurchaseReceiptEditorModel m&&m.Status==(byte)PurchaseReceiptStatus.Confirmed;
    private bool ShowCancelReceipt=>ActiveTab?.Model is UiPurchaseReceiptEditorModel m&&(m.Status is (byte)PurchaseReceiptStatus.Draft or (byte)PurchaseReceiptStatus.Confirmed);
    private bool ShowReceiptInventory=>ActiveTab?.Model is UiPurchaseReceiptEditorModel m&&m.Status==(byte)PurchaseReceiptStatus.Posted&&m.InventoryTransactionId.HasValue;
    private bool ShowReceiptJournal=>ActiveTab?.Model is UiPurchaseReceiptEditorModel m&&m.Status==(byte)PurchaseReceiptStatus.Posted&&m.JournalEntryId.HasValue;
    private bool ShowMatchInvoice=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&((m.Status is (byte)PurchaseInvoiceStatus.Draft or (byte)PurchaseInvoiceStatus.Confirmed or (byte)PurchaseInvoiceStatus.PendingMatchApproval))&&!ActiveTab.IsDirty;
    private bool ShowConfirmInvoice=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&m.Status==(byte)PurchaseInvoiceStatus.Draft&&!ActiveTab.IsDirty;
    private bool ShowApproveVariance=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&m.Status==(byte)PurchaseInvoiceStatus.PendingMatchApproval&&m.Match?.Allocations.Any(x=>x.Status=="يحتاج اعتماد")==true;
    private bool ShowPostInvoice=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&m.Status==(byte)PurchaseInvoiceStatus.Confirmed;
    private bool ShowCancelInvoice=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&(m.Status is (byte)PurchaseInvoiceStatus.Draft or (byte)PurchaseInvoiceStatus.Confirmed or (byte)PurchaseInvoiceStatus.PendingMatchApproval);
    private bool ShowInvoiceJournal=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&m.Status==(byte)PurchaseInvoiceStatus.Posted&&m.JournalEntryId.HasValue;

    private async Task SubmitRequestAsync(MouseEventArgs _)=>await ChangeRequestAsync((id,rv)=>Purchasing.SubmitPurchaseRequestAsync(id,new(rv)));
    private async Task ApproveRequestAsync(MouseEventArgs _)=>await ChangeRequestAsync((id,rv)=>Purchasing.ApprovePurchaseRequestAsync(id,new(rv)));
    private async Task RejectRequestAsync(MouseEventArgs _)
    {
        if(!await Dialog.ConfirmAsync("رفض طلب الشراء","سيتم رفض طلب الشراء الحالي.",AlertTone.Warning,"رفض","رجوع"))return;
        await ChangeRequestAsync((id,rv)=>Purchasing.RejectPurchaseRequestAsync(id,new(rv,"رفض من واجهة المشتريات")));
    }
    private async Task CancelRequestAsync(MouseEventArgs _)
    {
        if(!await Dialog.ConfirmAsync("إلغاء طلب الشراء","هل تريد إلغاء طلب الشراء؟",AlertTone.Warning,"إلغاء الطلب","رجوع"))return;
        await ChangeRequestAsync((id,rv)=>Purchasing.CancelPurchaseRequestAsync(id,new(rv,"إلغاء من واجهة المشتريات")));
    }
    private async Task ChangeRequestAsync(Func<Guid,string,Task<PurchaseRequestDto?>> action)
    {
        if(ActiveTab?.Model is not UiPurchaseRequestEditorModel m||m.Id is not Guid id)return;
        try{var dto=await action(id,m.RowVersion??string.Empty);if(dto is not null)SetActive(Map(dto));}
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task CreateOrderFromRequestAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseRequestEditorModel m||m.Id is not Guid id||m.WarehouseId is not Guid warehouse)return;
        var open=m.Lines.Where(x=>x.Id.HasValue&&x.RemainingQuantity>0).ToArray();
        var suppliers=open.Select(x=>x.PreferredSupplierId).Where(x=>x.HasValue).Select(x=>x!.Value).Distinct().ToArray();
        if(open.Length==0){Snackbar.Info("لا توجد كميات متبقية للتحويل.");return;}
        if(suppliers.Length!=1||open.Any(x=>x.PreferredSupplierId!=suppliers[0])){Snackbar.Warning("لإنشاء أمر شراء مباشر يجب تحديد مورد مفضل واحد لجميع البنود المتبقية.");return;}
        try
        {
            var settings=await Accounting.GetAccountingSettingsAsync();if(settings is null){Snackbar.Warning("يجب إعداد العملة الأساسية في المحاسبة أولاً.");return;}
            var req=new CreatePurchaseOrderFromRequestRequest(suppliers[0],warehouse,DateOnly.FromDateTime(DateTime.Today),m.RequiredDate,settings.BaseCurrencyId,1m,DateOnly.FromDateTime(DateTime.Today),TaxCalculationMode.Exclusive,0,m.Notes,open.Select(x=>new PurchaseRequestLineAllocationRequest(x.Id!.Value,x.RemainingQuantity)).ToArray(),m.RowVersion??string.Empty);
            var po=await Purchasing.CreatePurchaseOrderFromRequestAsync(id,req);if(po is null)return;
            var tab=Workspace.OpenRecord(PurchasingEntityType.PurchaseOrders,po.Id,$"{po.PurchaseOrderCode} - {po.SupplierName}");tab.Model=Map(po);tab.IsEditMode=false;tab.IsDirty=false;
            var refreshed=await Purchasing.GetPurchaseRequestAsync(id);if(refreshed is not null)SetActiveRequestIfCurrent(Map(refreshed));
            Navigation.NavigateTo("/purchases/orders");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }

    private async Task SubmitOrderAsync(MouseEventArgs _)=>await ChangeOrderAsync((id,rv)=>Purchasing.SubmitPurchaseOrderAsync(id,new(rv)));
    private async Task ApproveOrderAsync(MouseEventArgs _)=>await ChangeOrderAsync((id,rv)=>Purchasing.ApprovePurchaseOrderAsync(id,new(rv)));
    private async Task RejectOrderAsync(MouseEventArgs _)
    {
        if(!await Dialog.ConfirmAsync("رفض أمر الشراء","سيتم رفض أمر الشراء.",AlertTone.Warning,"رفض","رجوع"))return;
        await ChangeOrderAsync((id,rv)=>Purchasing.RejectPurchaseOrderAsync(id,new("رفض من واجهة المشتريات",rv)));
    }
    private async Task SendOrderAsync(MouseEventArgs _)=>await ChangeOrderAsync((id,rv)=>Purchasing.SendPurchaseOrderAsync(id,new(rv)));
    private async Task CancelOrderAsync(MouseEventArgs _)
    {
        if(!await Dialog.ConfirmAsync("إلغاء أمر الشراء","سيتم تحرير الكمية المفتوحة من OnOrder إذا كان الأمر مرسلاً.",AlertTone.Warning,"إلغاء الأمر","رجوع"))return;
        await ChangeOrderAsync((id,rv)=>Purchasing.CancelPurchaseOrderAsync(id,new("إلغاء من واجهة المشتريات",rv)));
    }
    private async Task CloseOrderAsync(MouseEventArgs _)=>await ChangeOrderAsync((id,rv)=>Purchasing.ClosePurchaseOrderAsync(id,new(rv)));
    private async Task ChangeOrderAsync(Func<Guid,string,Task<PurchaseOrderDto?>> action)
    {
        if(ActiveTab?.Model is not UiPurchaseOrderEditorModel m||m.Id is not Guid id)return;
        try{var dto=await action(id,m.RowVersion??string.Empty);if(dto is not null)SetActive(Map(dto));}
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task CreateReceiptFromOrderAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseOrderEditorModel po||po.Id is not Guid id)return;
        var m=new UiPurchaseReceiptEditorModel{PurchaseOrderId=id,PurchaseOrderCode=po.PurchaseOrderCode,SupplierId=po.SupplierId,SupplierName=po.SupplierName,WarehouseId=po.DestinationWarehouseId,WarehouseName=po.DestinationWarehouseName};
        foreach(var line in po.Lines.Where(x=>x.RemainingBaseQuantity>0))
        {
            var factor=line.UnitConversionFactor<=0?1m:line.UnitConversionFactor;
            var remaining=Math.Round(line.RemainingBaseQuantity/factor,3);
            m.Lines.Add(new(){PurchaseOrderLineId=line.Id!.Value,LineSequence=m.Lines.Count+1,ProductVariantId=line.ProductVariantId!.Value,ProductName=line.ProductName,OrderedQuantitySnapshot=line.OrderedQuantity,PreviouslyReceivedQty=Math.Max(0,line.OrderedQuantity-remaining),RemainingReceivableQuantity=remaining,ActualUnitCost=Math.Round(line.UnitPrice/factor,4)});
        }
        var tab=Workspace.OpenNew(PurchasingEntityType.PurchaseReceipts,"استلام جديد");tab.Model=m;tab.IsDirty=false;tab.IsEditMode=true;Navigation.NavigateTo("/purchases/receipts");await InvokeAsync(StateHasChanged);
    }

    private async Task ConfirmReceiptAsync(MouseEventArgs _)=>await ChangeReceiptAsync((id,rv)=>Purchasing.ConfirmPurchaseReceiptAsync(id,new(rv)));
    private async Task CancelReceiptAsync(MouseEventArgs _)
    {
        if(!await Dialog.ConfirmAsync("إلغاء الاستلام","هل تريد إلغاء سند الاستلام؟",AlertTone.Warning,"إلغاء","رجوع"))return;
        await ChangeReceiptAsync((id,rv)=>Purchasing.CancelPurchaseReceiptAsync(id,new("إلغاء من واجهة المشتريات",rv)));
    }
    private async Task ChangeReceiptAsync(Func<Guid,string,Task<PurchaseReceiptDto?>> action)
    {
        if(ActiveTab?.Model is not UiPurchaseReceiptEditorModel m||m.Id is not Guid id)return;
        try{var dto=await action(id,m.RowVersion??string.Empty);if(dto is not null)SetActive(Map(dto));}
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task PostReceiptAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseReceiptEditorModel m||m.Id is not Guid id)return;
        if(!await Dialog.ConfirmAsync("ترحيل الاستلام","سيتم تحديث المخزون وإنشاء قيد Inventory / GRNI داخل عملية واحدة.",AlertTone.Warning,"ترحيل","رجوع"))return;
        try{await Purchasing.PostPurchaseReceiptAsync(id,new(m.RowVersion??string.Empty));var dto=await Purchasing.GetPurchaseReceiptAsync(id);if(dto is not null)SetActive(Map(dto));Snackbar.Success("تم ترحيل الاستلام وتحديث المخزون والمحاسبة.");}
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }

    private async Task ConfirmInvoiceAsync(MouseEventArgs _)=>await ChangeInvoiceAsync((id,rv)=>Purchasing.ConfirmPurchaseInvoiceAsync(id,new(rv)));
    private async Task CancelInvoiceAsync(MouseEventArgs _)
    {
        if(!await Dialog.ConfirmAsync("إلغاء فاتورة المشتريات","هل تريد إلغاء الفاتورة قبل الترحيل؟",AlertTone.Warning,"إلغاء","رجوع"))return;
        await ChangeInvoiceAsync((id,rv)=>Purchasing.CancelPurchaseInvoiceAsync(id,new("إلغاء من واجهة المشتريات",rv)));
    }
    private async Task ChangeInvoiceAsync(Func<Guid,string,Task<PurchaseInvoiceDto?>> action)
    {
        if(ActiveTab?.Model is not UiPurchaseInvoiceEditorModel m||m.Id is not Guid id)return;
        try{var dto=await action(id,m.RowVersion??string.Empty);if(dto is not null)SetActive(Map(dto));}
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task MatchInvoiceAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseInvoiceEditorModel m||m.Id is not Guid id||m.SupplierId is not Guid supplier)return;
        try
        {
            var page=await Purchasing.GetPurchaseReceiptsAsync(new PageRequest{PageNumber=1,PageSize=100,SortBy="PostingDate",SortDirection=SortDirection.Descending},PurchaseReceiptStatus.Posted,null,supplier);
            var candidates=page.Items.SelectMany(x=>x.Lines).ToList();var used=new HashSet<Guid>();var allocations=new List<PurchaseInvoiceMatchAllocationRequest>();
            foreach(var line in m.Lines.Where(x=>x.Id.HasValue&&x.ProductVariantId.HasValue))
            {
                var remaining=line.Quantity;
                foreach(var r in candidates.Where(x=>!used.Contains(x.Id)&&x.ProductVariantId==line.ProductVariantId&&(!line.PurchaseOrderLineId.HasValue||x.PurchaseOrderLineId==line.PurchaseOrderLineId)).OrderBy(x=>x.LineSequence))
                {
                    if(remaining<=0)break;var available=Math.Max(0,r.AcceptedQuantity);if(available<=0)continue;var qty=Math.Min(remaining,available);allocations.Add(new(line.Id!.Value,r.Id,qty));used.Add(r.Id);remaining-=qty;
                }
                if(remaining>0){Snackbar.Warning($"لا توجد كمية استلام مرحلة كافية لمطابقة البند {line.LineSequence}.");return;}
            }
            if(allocations.Count==0){Snackbar.Warning("لا توجد استلامات مرحلة مناسبة للمطابقة.");return;}
            var result=await Purchasing.MatchPurchaseInvoiceAsync(id,new(allocations,m.RowVersion??string.Empty));if(result is null)return;
            var refreshed=await Purchasing.GetPurchaseInvoiceAsync(id);if(refreshed is null)return;var mapped=Map(refreshed);mapped.Match=Map(result);SetActive(mapped);Snackbar.Success("تم تشغيل المطابقة الثلاثية.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task ApproveVariancesAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseInvoiceEditorModel m||m.Id is not Guid id||m.Match is null)return;
        var pending=m.Match.Allocations.Where(x=>x.Status=="يحتاج اعتماد"&&!string.IsNullOrWhiteSpace(x.RowVersion)).ToArray();
        if(m.Match.Allocations.Any(x=>x.Status=="يحتاج اعتماد"&&string.IsNullOrWhiteSpace(x.RowVersion))){Snackbar.Warning("تعذر اعتماد بعض الفروقات لأن نسخة المطابقة غير متاحة. شغّل المطابقة مرة أخرى.");return;}
        if(pending.Length==0)return;
        if(!await Dialog.ConfirmAsync("اعتماد فروقات المطابقة",$"سيتم اعتماد {pending.Length} فرق/فروقات بعد المراجعة.",AlertTone.Warning,"اعتماد","رجوع"))return;
        try
        {
            foreach(var a in pending){var dto=await Purchasing.ApprovePurchaseVarianceAsync(id,new(a.Id,"تمت مراجعة الفرق واعتماده من واجهة المشتريات",a.RowVersion!,m.RowVersion??string.Empty));if(dto is not null)m.RowVersion=dto.RowVersion;}
            var refreshed=await Purchasing.GetPurchaseInvoiceAsync(id);if(refreshed is not null)SetActive(Map(refreshed));Snackbar.Success("تم اعتماد الفروقات.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task PostInvoiceAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseInvoiceEditorModel m||m.Id is not Guid id)return;
        if(!await Dialog.ConfirmAsync("ترحيل فاتورة المورد","سيتم إنشاء قيد GRNI / الضريبة / فروقات السعر / حساب المورد.",AlertTone.Warning,"ترحيل","رجوع"))return;
        try{await Purchasing.PostPurchaseInvoiceAsync(id,new(m.RowVersion??string.Empty));var dto=await Purchasing.GetPurchaseInvoiceAsync(id);if(dto is not null)SetActive(Map(dto));Snackbar.Success("تم ترحيل فاتورة المورد.");}
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }

    private Task OpenReceiptInventoryAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is UiPurchaseReceiptEditorModel { InventoryTransactionId: Guid id })
            Navigation.NavigateTo($"/inventory/transactions?transactionId={id:D}");
        return Task.CompletedTask;
    }

    private Task OpenReceiptJournalAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is UiPurchaseReceiptEditorModel { JournalEntryId: Guid id })
            Navigation.NavigateTo($"/accounting/journals?journalId={id:D}");
        return Task.CompletedTask;
    }

    private Task OpenInvoiceJournalAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is UiPurchaseInvoiceEditorModel { JournalEntryId: Guid id })
            Navigation.NavigateTo($"/accounting/journals?journalId={id:D}");
        return Task.CompletedTask;
    }

    private async Task AddCatalogPriceAsync()
    {
        if(ActiveTab?.Model is not UiSupplierCatalogEditorModel m||m.Id is not Guid id){Snackbar.Info("احفظ عنصر كتالوج المورد أولاً قبل إضافة السعر.");return;}
        if(m.NewPriceCurrencyId is not Guid currency){Snackbar.Warning("حدد عملة السعر.");return;}
        try
        {
            var dto=await Purchasing.AddSupplierPriceAsync(id,new(currency,m.NewPriceUnitPrice,m.NewPriceEffectiveFrom,m.NewPriceEffectiveTo,true,m.NewPriceNotes));
            if(dto is not null)SetActive(Map(dto));
            Snackbar.Success("تمت إضافة السعر إلى سجل أسعار المورد.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }

    private void SetActive(object model){if(ActiveTab is not { } t)return;t.Model=model;t.Title=ModelTitle(model);t.IsDirty=false;t.IsEditMode=false;StateHasChanged();}
    private void SetActiveRequestIfCurrent(UiPurchaseRequestEditorModel model){var tab=Workspace.Tabs.FirstOrDefault(x=>x.EntityType==PurchasingEntityType.PurchaseRequests&&x.EntityId==model.Id);if(tab is null)return;tab.Model=model;tab.Title=model.RequestCode;tab.IsDirty=false;tab.IsEditMode=false;}
}
