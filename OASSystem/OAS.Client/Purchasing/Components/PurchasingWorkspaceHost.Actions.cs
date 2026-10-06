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
    private static decimal CalculateBaseReceiptUnitCost(UiPurchaseOrderLineModel line, decimal exchangeRate)
        => CalculateBaseReceiptUnitCost(
            line.UnitConversionFactor,
            line.OrderedQuantity,
            line.UnitPrice,
            line.DiscountAmount,
            line.NetAmount,
            exchangeRate);

    private static decimal CalculateBaseReceiptUnitCost(PurchaseOrderLineDto line, decimal exchangeRate)
        => CalculateBaseReceiptUnitCost(
            line.UnitConversionFactor,
            line.OrderedQuantity,
            line.UnitPrice,
            line.DiscountAmount,
            line.NetAmount,
            exchangeRate);

    private static decimal CalculateBaseReceiptUnitCost(
        decimal unitConversionFactor,
        decimal orderedQuantity,
        decimal unitPrice,
        decimal discountAmount,
        decimal netAmount,
        decimal exchangeRate)
    {
        var factor = unitConversionFactor <= 0m ? 1m : unitConversionFactor;
        var quantity = orderedQuantity <= 0m ? 1m : orderedQuantity;
        var rate = exchangeRate <= 0m ? 1m : exchangeRate;

        // Inventory/GRNI cost is stored per base inventory unit and in base currency.
        // Use PO net amount (after discount, excluding recoverable tax) rather than gross unit price.
        var netPurchaseUnitCost = netAmount > 0m
            ? netAmount / quantity
            : Math.Max(0m, unitPrice - (discountAmount / quantity));

        return Math.Round((netPurchaseUnitCost * rate) / factor, 4);
    }
    private bool ShowSubmitRequest=>ActiveTab?.Model is UiPurchaseRequestEditorModel m&&m.Status==(byte)PurchaseRequestStatus.Draft&&!ActiveTab.IsDirty;
    private bool ShowApproveRequest=>ActiveTab?.Model is UiPurchaseRequestEditorModel m&&m.Status==(byte)PurchaseRequestStatus.PendingApproval;
    private bool ShowCancelRequest=>ActiveTab?.Model is UiPurchaseRequestEditorModel m&&(m.Status is (byte)PurchaseRequestStatus.Draft or (byte)PurchaseRequestStatus.Approved or (byte)PurchaseRequestStatus.PartiallyConverted);
    private string CancelRequestText=>ActiveTab?.Model is UiPurchaseRequestEditorModel m&&m.Status==(byte)PurchaseRequestStatus.PartiallyConverted?"إلغاء المتبقي":"إلغاء الطلب";
    private bool ShowCreateOrderFromRequest=>ActiveTab?.Model is UiPurchaseRequestEditorModel m&&(m.Status is (byte)PurchaseRequestStatus.Approved or (byte)PurchaseRequestStatus.PartiallyConverted);
    private bool ShowSubmitOrder=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&m.Status==(byte)PurchaseOrderStatus.Draft&&!ActiveTab.IsDirty;
    private bool ShowApproveOrder=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&m.Status==(byte)PurchaseOrderStatus.PendingApproval;
    private bool ShowSendOrder=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&m.Status==(byte)PurchaseOrderStatus.Approved;
    private bool ShowCreateReceipt=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&(m.Status is (byte)PurchaseOrderStatus.Sent or (byte)PurchaseOrderStatus.PartiallyReceived);
    private bool ShowCloseOrder=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&(m.Status is (byte)PurchaseOrderStatus.FullyReceived or (byte)PurchaseOrderStatus.PartiallyReceived);
    private string CloseOrderText=>ActiveTab?.Model is UiPurchaseOrderEditorModel m&&m.Status==(byte)PurchaseOrderStatus.PartiallyReceived?"إغلاق المتبقي":"إغلاق الأمر";
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
        var partial=ActiveTab?.Model is UiPurchaseRequestEditorModel m&&m.Status==(byte)PurchaseRequestStatus.PartiallyConverted;
        var title=partial?"إلغاء الكمية المتبقية":"إلغاء طلب الشراء";
        var message=partial?"سيتم إلغاء الكمية غير المحولة فقط، ولن تتأثر أوامر الشراء التي تم إنشاؤها سابقًا.":"هل تريد إلغاء طلب الشراء؟";
        var confirm=partial?"إلغاء المتبقي":"إلغاء الطلب";
        if(!await Dialog.ConfirmAsync(title,message,AlertTone.Warning,confirm,"رجوع"))return;
        var reason=partial?"إلغاء الكمية المتبقية من واجهة المشتريات":"إلغاء من واجهة المشتريات";
        await ChangeRequestAsync((id,rv)=>Purchasing.CancelPurchaseRequestAsync(id,new(rv,reason)));
    }
    private async Task ChangeRequestAsync(Func<Guid,string,Task<PurchaseRequestDto?>> action)
    {
        if(ActiveTab?.Model is not UiPurchaseRequestEditorModel m||m.Id is not Guid id)return;
        try{var dto=await action(id,m.RowVersion??string.Empty);if(dto is not null)SetActive(Map(dto));}
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task CreateOrderFromRequestAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseRequestEditorModel request||request.Id is not Guid||request.WarehouseId is not Guid warehouse)return;
        var open=request.Lines.Where(x=>x.Id.HasValue&&x.RemainingQuantity>0m).OrderBy(x=>x.LineSequence).ToArray();
        if(open.Length==0){Snackbar.Info("لا توجد كميات متبقية للتحويل.");return;}

        try
        {
            var settings=await Accounting.GetAccountingSettingsAsync();
            if(settings is null||settings.BaseCurrencyId==Guid.Empty){Snackbar.Warning("يجب إعداد العملة الأساسية في المحاسبة أولاً.");return;}

            var today=DateOnly.FromDateTime(DateTime.Today);
            var preferredSuppliers=open.Where(x=>x.PreferredSupplierId.HasValue).Select(x=>x.PreferredSupplierId!.Value).Distinct().ToArray();
            var defaultSupplierId=preferredSuppliers.Length==1?preferredSuppliers[0]:(Guid?)null;
            var defaultSupplierName=defaultSupplierId.HasValue?open.FirstOrDefault(x=>x.PreferredSupplierId==defaultSupplierId)?.PreferredSupplierName:null;

            var draft=new UiPurchaseOrderEditorModel
            {
                PurchaseOrderCode="يولد عند الحفظ",
                Status=(byte)PurchaseOrderStatus.Draft,
                StatusText="مسودة",
                SupplierId=defaultSupplierId,
                SupplierName=defaultSupplierName,
                DestinationWarehouseId=warehouse,
                DestinationWarehouseName=request.WarehouseName,
                OrderDate=today,
                ExpectedDeliveryDate=request.RequiredDate,
                CurrencyId=settings.BaseCurrencyId,
                CurrencyCode=settings.BaseCurrencyCode,
                ExchangeRate=1m,
                ExchangeRateDate=today,
                TaxCalculationMode=(byte)TaxCalculationMode.Exclusive,
                PaymentTermDays=0,
                Notes=request.Notes
            };

            foreach(var source in open)
            {
                var line=new UiPurchaseOrderLineModel
                {
                    LineSequence=draft.Lines.Count+1,
                    ProductVariantId=source.ProductVariantId,
                    ProductName=source.ProductName,
                    UnitConversionFactor=1m,
                    OrderedQuantity=source.RemainingQuantity,
                    UnitPrice=0m,
                    DiscountAmount=0m,
                    TaxRate=0m,
                    ExpectedDeliveryDate=source.RequiredDate??request.RequiredDate,
                    Notes=source.Notes
                };
                line.Sources.Add(new UiPurchaseOrderLineSourceModel
                {
                    PurchaseRequestLineId=source.Id!.Value,
                    AllocatedQuantity=source.RemainingQuantity
                });
                draft.Lines.Add(line);
            }

            if(defaultSupplierId.HasValue)
            {
                var supplier=await Accounting.GetSupplierByIdAsync(defaultSupplierId.Value);
                if(supplier is not null)
                {
                    draft.SupplierName=supplier.NameAr;
                    draft.PaymentTermDays=Math.Max(0,supplier.PaymentTermDays);
                }
                await ApplySupplierCatalogDefaultsAsync(draft,defaultSupplierId.Value,resetCommercialDefaults:false);
            }

            var tab=Workspace.OpenNew(PurchasingEntityType.PurchaseOrders,$"أمر شراء من {request.RequestCode}");
            tab.Model=draft;tab.IsEditMode=true;tab.IsDirty=false;
            Navigation.NavigateTo("/purchases/orders");

            var missingUnits=draft.Lines.Count(x=>!x.PurchaseUnitId.HasValue);
            var missingPrices=draft.Lines.Count(x=>x.UnitPrice<=0m);
            if(!defaultSupplierId.HasValue)
                Snackbar.Info("تم تجهيز مسودة أمر الشراء من الطلب. اختر المورد ثم أكمل الوحدة والسعر قبل الحفظ.");
            else if(missingUnits>0)
                Snackbar.Warning($"تم تجهيز المسودة، ويوجد {missingUnits} بند/بنود بدون وحدة شراء. أكمل الوحدة ومعامل التحويل قبل الحفظ.");
            else if(missingPrices>0)
                Snackbar.Warning($"تم تجهيز المسودة من كتالوج المورد، ويوجد {missingPrices} بند/بنود بدون سعر حالي. أدخل السعر قبل المتابعة.");
            else
                Snackbar.Success("تم تجهيز مسودة أمر الشراء من طلب الشراء. راجع البيانات ثم احفظها.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }

    private async Task PurchaseOrderSupplierChangedAsync(Guid? supplierId)
    {
        if(ActiveTab?.Model is not UiPurchaseOrderEditorModel order)return;
        if(!supplierId.HasValue)
        {
            order.SupplierName=null;
            foreach(var line in order.Lines)
            {
                line.SupplierCatalogItemId=null;line.PurchaseUnitId=null;line.PurchaseUnitName=null;line.UnitConversionFactor=1m;line.UnitPrice=0m;
                RecalculateSourcedOrderQuantity(line);
            }
            if(ActiveTab is { } emptyTab)emptyTab.IsDirty=true;
            StateHasChanged();
            return;
        }

        try
        {
            var supplier=await Accounting.GetSupplierByIdAsync(supplierId.Value);
            if(supplier is not null)
            {
                order.SupplierName=supplier.NameAr;
                order.PaymentTermDays=Math.Max(0,supplier.PaymentTermDays);
            }
            await ApplySupplierCatalogDefaultsAsync(order,supplierId.Value,resetCommercialDefaults:true);
            if(ActiveTab is { } tab)tab.IsDirty=true;
            var missing=order.Lines.Count(x=>!x.PurchaseUnitId.HasValue);
            if(missing>0)Snackbar.Info($"لم يوجد كتالوج للمورد لبعض البنود ({missing}). اختر وحدة الشراء وأدخل السعر يدويًا لهذه البنود.");
            StateHasChanged();
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }


    private async Task PurchaseOrderDateChangedAsync(DateOnly orderDate)
    {
        if(ActiveTab?.Model is not UiPurchaseOrderEditorModel order)return;
        order.OrderDate=orderDate;
        if(order.SupplierId.HasValue&&order.CurrencyId.HasValue)
        {
            try{await RefreshSupplierPricesAsync(order,order.SupplierId.Value);}
            catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
        }
        if(ActiveTab is { } tab)tab.IsDirty=true;
        StateHasChanged();
    }

    private async Task PurchaseOrderCurrencyChangedAsync(Guid? currencyId)
    {
        if(ActiveTab?.Model is not UiPurchaseOrderEditorModel order||!currencyId.HasValue)return;
        try
        {
            var currency=await Accounting.GetCurrencyByIdAsync(currencyId.Value);
            if(currency is not null)order.CurrencyCode=currency.Code;
            var settings=await Accounting.GetAccountingSettingsAsync();
            if(settings is not null&&settings.BaseCurrencyId==currencyId.Value)order.ExchangeRate=1m;
            if(order.SupplierId.HasValue)await RefreshSupplierPricesAsync(order,order.SupplierId.Value);
            if(ActiveTab is { } tab)tab.IsDirty=true;
            StateHasChanged();
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }

    private async Task ApplySupplierCatalogDefaultsAsync(UiPurchaseOrderEditorModel order,Guid supplierId,bool resetCommercialDefaults)
    {
        foreach(var line in order.Lines.Where(x=>x.ProductVariantId.HasValue))
        {
            if(resetCommercialDefaults)
            {
                line.SupplierCatalogItemId=null;line.PurchaseUnitId=null;line.PurchaseUnitName=null;line.UnitConversionFactor=1m;line.UnitPrice=0m;
                RecalculateSourcedOrderQuantity(line);
            }

            var page=await Purchasing.GetSupplierCatalogAsync(new PageRequest{PageNumber=1,PageSize=24,SortDirection=SortDirection.Ascending},supplierId,line.ProductVariantId,true);
            var catalog=page.Items.OrderByDescending(x=>x.IsPreferred).ThenBy(x=>x.SupplierProductCode).FirstOrDefault();
            if(catalog is null)continue;

            line.SupplierCatalogItemId=catalog.Id;
            line.PurchaseUnitId=catalog.PurchaseUnitId;
            line.PurchaseUnitName=catalog.PurchaseUnitName;
            line.UnitConversionFactor=catalog.UnitConversionFactor>0m?catalog.UnitConversionFactor:1m;
            RecalculateSourcedOrderQuantity(line);
            line.UnitPrice=FindEffectiveSupplierPrice(catalog,order.CurrencyId,order.OrderDate)??0m;
        }
    }

    private async Task RefreshSupplierPricesAsync(UiPurchaseOrderEditorModel order,Guid supplierId)
    {
        foreach(var line in order.Lines.Where(x=>x.ProductVariantId.HasValue))
        {
            var page=await Purchasing.GetSupplierCatalogAsync(new PageRequest{PageNumber=1,PageSize=24,SortDirection=SortDirection.Ascending},supplierId,line.ProductVariantId,true);
            var catalog=line.SupplierCatalogItemId.HasValue?page.Items.FirstOrDefault(x=>x.Id==line.SupplierCatalogItemId.Value):null;
            catalog??=line.PurchaseUnitId.HasValue?page.Items
                .Where(x=>x.PurchaseUnitId==line.PurchaseUnitId.Value&&x.UnitConversionFactor==line.UnitConversionFactor)
                .OrderByDescending(x=>x.IsPreferred).FirstOrDefault():null;
            line.UnitPrice=catalog is null?0m:(FindEffectiveSupplierPrice(catalog,order.CurrencyId,order.OrderDate)??0m);
        }
    }

    private static decimal? FindEffectiveSupplierPrice(OAS.Contracts.Purchasing.SupplierCatalog.SupplierCatalogItemDto catalog,Guid? currencyId,DateOnly documentDate)
        => !currencyId.HasValue?null:catalog.PriceHistory
            .Where(x=>x.CurrencyId==currencyId.Value&&x.EffectiveFrom<=documentDate&&(!x.EffectiveTo.HasValue||x.EffectiveTo.Value>=documentDate))
            .OrderByDescending(x=>x.IsCurrent)
            .ThenByDescending(x=>x.EffectiveFrom)
            .Select(x=>(decimal?)x.UnitPrice)
            .FirstOrDefault();

    private static void RecalculateSourcedOrderQuantity(UiPurchaseOrderLineModel line)
    {
        if(line.Sources.Count==0)return;
        var baseQuantity=line.Sources.Sum(x=>Math.Max(0m,x.AllocatedQuantity));
        var factor=line.UnitConversionFactor<=0m?1m:line.UnitConversionFactor;
        line.OrderedQuantity=ToPurchaseQuantity(baseQuantity,factor);
        line.BaseQuantity=baseQuantity;
    }

    private static decimal ToPurchaseQuantity(decimal baseQuantity,decimal factor)
    {
        if(baseQuantity<=0m)return 0m;
        var safeFactor=factor<=0m?1m:factor;
        return Math.Ceiling((baseQuantity/safeFactor)*1000m)/1000m;
    }

    private async Task PurchaseOrderLineUnitChangedAsync(UiPurchaseOrderLineModel line)
    {
        if(!line.PurchaseUnitId.HasValue){line.PurchaseUnitName=null;return;}
        try
        {
            var unit=await Inventory.GetUnitAsync(line.PurchaseUnitId.Value);
            line.PurchaseUnitName=unit?.NameAr;
            if(ActiveTab is { } tab)tab.IsDirty=true;
            StateHasChanged();
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
        private async Task CloseOrderAsync(MouseEventArgs _)
    {
        var partial=ActiveTab?.Model is UiPurchaseOrderEditorModel m&&m.Status==(byte)PurchaseOrderStatus.PartiallyReceived;
        if(partial&&!await Dialog.ConfirmAsync("إغلاق الكمية المتبقية","سيتم تحرير الكمية المفتوحة المتبقية من OnOrder ثم إغلاق أمر الشراء. الاستلامات والفواتير المرحلة لن تتغير.",AlertTone.Warning,"إغلاق المتبقي","رجوع"))return;
        await ChangeOrderAsync((id,rv)=>Purchasing.ClosePurchaseOrderAsync(id,new(rv)));
    }
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
            m.Lines.Add(new(){PurchaseOrderLineId=line.Id!.Value,LineSequence=m.Lines.Count+1,ProductVariantId=line.ProductVariantId!.Value,ProductName=line.ProductName,OrderedQuantitySnapshot=line.OrderedQuantity,PreviouslyReceivedQty=Math.Max(0,line.OrderedQuantity-remaining),RemainingReceivableQuantity=remaining,ActualUnitCost=CalculateBaseReceiptUnitCost(line, po.ExchangeRate)});
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
        try
        {
            var accountingSettings=await Accounting.GetAccountingSettingsAsync();
            if(accountingSettings is null)
            {
                Snackbar.Warning("يجب إعداد المحاسبة قبل ترحيل استلام المشتريات.");
                return;
            }
            if(!accountingSettings.InventoryAccountId.HasValue||!accountingSettings.GrniAccountId.HasValue)
            {
                Snackbar.Warning("أكمل إعداد ترحيل المشتريات أولاً: حدد حساب المخزون وحساب بضاعة مستلمة غير مفوترة (GRNI) من إعدادات المحاسبة ثم احفظ.");
                return;
            }

            if(!await Dialog.ConfirmAsync("ترحيل الاستلام","سيتم تحديث المخزون وإنشاء قيد Inventory / GRNI داخل عملية واحدة.",AlertTone.Warning,"ترحيل","رجوع"))return;
            await Purchasing.PostPurchaseReceiptAsync(id,new(m.RowVersion??string.Empty));
            var dto=await Purchasing.GetPurchaseReceiptAsync(id);
            if(dto is not null)SetActive(Map(dto));
            Snackbar.Success("تم ترحيل الاستلام وتحديث المخزون والمحاسبة.");
        }
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
            var candidates=page.Items.SelectMany(x=>x.Lines).ToList();
            var remainingByReceiptLine=candidates.ToDictionary(x=>x.Id,x=>Math.Max(0m,x.AcceptedQuantity-x.ReturnedQuantity));
            var allocations=new List<PurchaseInvoiceMatchAllocationRequest>();
            foreach(var line in m.Lines.Where(x=>x.Id.HasValue&&x.ProductVariantId.HasValue))
            {
                var remaining=line.Quantity;
                foreach(var r in candidates.Where(x=>x.ProductVariantId==line.ProductVariantId&&(!line.PurchaseOrderLineId.HasValue||x.PurchaseOrderLineId==line.PurchaseOrderLineId)).OrderBy(x=>x.LineSequence))
                {
                    if(remaining<=0)break;
                    var available=remainingByReceiptLine[r.Id];
                    if(available<=0)continue;
                    var qty=Math.Min(remaining,available);
                    allocations.Add(new(line.Id!.Value,r.Id,qty));
                    remainingByReceiptLine[r.Id]=available-qty;
                    remaining-=qty;
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
