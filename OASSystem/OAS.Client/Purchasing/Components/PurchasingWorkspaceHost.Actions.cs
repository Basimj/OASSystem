using OAS.Client.Services.Http;
using Microsoft.AspNetCore.Components.Web;
using OAS.Client.Purchasing.Common;
using OAS.Client.Purchasing.Workspace;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Contracts.Purchasing.PurchaseRequests;
using OAS.Contracts.Sales.Enums;
using OAS.UiLib.Core.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Components.Purchasing;
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
    private bool ShowReopenReceipt=>ActiveTab?.Model is UiPurchaseReceiptEditorModel m&&m.Status==(byte)PurchaseReceiptStatus.Confirmed;
    private bool ShowCancelReceipt=>ActiveTab?.Model is UiPurchaseReceiptEditorModel m&&(m.Status is (byte)PurchaseReceiptStatus.Draft or (byte)PurchaseReceiptStatus.Confirmed);
    private bool ShowReceiptInventory=>ActiveTab?.Model is UiPurchaseReceiptEditorModel m&&m.Status==(byte)PurchaseReceiptStatus.Posted&&m.InventoryTransactionId.HasValue;
    private bool ShowReceiptJournal=>ActiveTab?.Model is UiPurchaseReceiptEditorModel m&&m.Status==(byte)PurchaseReceiptStatus.Posted&&m.JournalEntryId.HasValue;
    private bool ShowCreateInvoiceFromReceipt=>ActiveTab?.Model is UiPurchaseReceiptEditorModel m&&m.Status==(byte)PurchaseReceiptStatus.Posted;
    private bool ShowMatchInvoice=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&((m.Status is (byte)PurchaseInvoiceStatus.Draft or (byte)PurchaseInvoiceStatus.Confirmed or (byte)PurchaseInvoiceStatus.PendingMatchApproval))&&!ActiveTab.IsDirty;
    private bool ShowConfirmInvoice=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&m.Status==(byte)PurchaseInvoiceStatus.Draft&&!ActiveTab.IsDirty;
    private bool ShowApproveVariance=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&m.Status==(byte)PurchaseInvoiceStatus.PendingMatchApproval&&m.Match?.Allocations.Any(x=>x.Status=="يحتاج اعتماد")==true;
    private bool ShowPostInvoice=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&m.Status==(byte)PurchaseInvoiceStatus.Confirmed;
    private bool ShowCancelInvoice=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&(m.Status is (byte)PurchaseInvoiceStatus.Draft or (byte)PurchaseInvoiceStatus.Confirmed or (byte)PurchaseInvoiceStatus.PendingMatchApproval);
    private bool ShowInvoiceJournal=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&m.Status==(byte)PurchaseInvoiceStatus.Posted&&m.JournalEntryId.HasValue;
    private bool ShowPrintInvoice=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&m.Status==(byte)PurchaseInvoiceStatus.Posted&&m.Id.HasValue&&!ActiveTab.IsDirty;
    private bool ShowPayInvoice=>ActiveTab?.Model is UiPurchaseInvoiceEditorModel m&&m.Status==(byte)PurchaseInvoiceStatus.Posted&&m.Id.HasValue;

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
            line.PurchaseUnitName=unit?.Name;
            if(ActiveTab is { } tab)tab.IsDirty=true;
            StateHasChanged();
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }

    private bool ValidateOrderPricesBeforeTransition()
    {
        if (ActiveTab?.Model is not UiPurchaseOrderEditorModel order) return false;
        var valid = _purchaseOrderEditor is not null && ReferenceEquals(_purchaseOrderEditor.Model, order)
            ? _purchaseOrderEditor.ValidateForSubmission()
            : UiPurchaseOrderFormValidator.Validate(order, requirePricedLines: true).Count == 0;
        if (!valid)
            Snackbar.Warning("لا يمكن إرسال أمر الشراء للاعتماد أو اعتماده أو إرساله للمورد: راجع سعر الوحدة وصافي التكلفة في كل بند.");
        return valid;
    }

    private async Task SubmitOrderAsync(MouseEventArgs _)
    {
        if (!ValidateOrderPricesBeforeTransition()) return;
        await ChangeOrderAsync((id,rv)=>Purchasing.SubmitPurchaseOrderAsync(id,new(rv)));
    }
    private async Task ApproveOrderAsync(MouseEventArgs _)
    {
        if (!ValidateOrderPricesBeforeTransition()) return;
        await ChangeOrderAsync((id,rv)=>Purchasing.ApprovePurchaseOrderAsync(id,new(rv)));
    }
    private async Task RejectOrderAsync(MouseEventArgs _)
    {
        if(!await Dialog.ConfirmAsync("رفض أمر الشراء","سيتم رفض أمر الشراء.",AlertTone.Warning,"رفض","رجوع"))return;
        await ChangeOrderAsync((id,rv)=>Purchasing.RejectPurchaseOrderAsync(id,new("رفض من واجهة المشتريات",rv)));
    }
    private async Task SendOrderAsync(MouseEventArgs _)
    {
        if (!ValidateOrderPricesBeforeTransition()) return;
        await ChangeOrderAsync((id,rv)=>Purchasing.SendPurchaseOrderAsync(id,new(rv)));
    }
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
            m.Lines.Add(new(){PurchaseOrderLineId=line.Id!.Value,LineSequence=m.Lines.Count+1,ProductVariantId=line.ProductVariantId!.Value,ProductName=line.ProductName,PurchaseUnitName=line.PurchaseUnitName,OrderedQuantitySnapshot=line.OrderedQuantity,PreviouslyReceivedQty=Math.Max(0,line.OrderedQuantity-remaining),RemainingReceivableQuantity=remaining,ActualUnitCost=CalculateBaseReceiptUnitCost(line, po.ExchangeRate)});
        }
        var tab=Workspace.OpenNew(PurchasingEntityType.PurchaseReceipts,"استلام جديد");tab.Model=m;tab.IsDirty=false;tab.IsEditMode=true;Navigation.NavigateTo("/purchases/receipts");await InvokeAsync(StateHasChanged);
    }

    private async Task ConfirmReceiptAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiPurchaseReceiptEditorModel receipt) return;
        if (_purchaseReceiptEditor is not null && !_purchaseReceiptEditor.ValidateForPosting())
        {
            Snackbar.Warning("قيمة الاستلام صفر؛ راجع التكلفة الفعلية المحددة بالأحمر، ثم عدّل السند واحفظه قبل التأكيد.");
            return;
        }
        await ChangeReceiptAsync((id,rv)=>Purchasing.ConfirmPurchaseReceiptAsync(id,new(rv)));
    }
    private async Task ReopenReceiptAsync(MouseEventArgs _)
    {
        if (ActiveTab?.Model is not UiPurchaseReceiptEditorModel receipt || receipt.Id is not Guid id) return;
        if (!await Dialog.ConfirmAsync("إعادة سند الاستلام إلى المسودة",
            "سيتم إلغاء تأكيد السند غير المرحّل كي تتمكن من تعديل التكلفة الفعلية ثم حفظه وتأكيده مجددًا. هل تريد المتابعة؟",
            AlertTone.Warning, "إعادة إلى المسودة", "رجوع")) return;
        try
        {
            var dto = await Purchasing.ReopenPurchaseReceiptAsync(id, new(receipt.RowVersion??string.Empty));
            if (dto is not null)
            {
                SetActive(Map(dto));
                if (ActiveTab is { } tab) tab.IsEditMode = true;
                StateHasChanged();
                Snackbar.Success("أعيد الاستلام إلى المسودة. صحح التكلفة الفعلية، ثم احفظ السند وأكده مجددًا.");
            }
        }
        catch (ApiClientException ex) { ApiFeedback.Show(ex.Error); }
        catch { ApiFeedback.ShowUnexpected(); }
    }
    private async Task CancelReceiptAsync(MouseEventArgs _)
    {
        if(!await Dialog.ConfirmAsync("إلغاء الاستلام","هل تريد إلغاء سند الاستلام؟",AlertTone.Warning,"إلغاء","رجوع"))return;
        await ChangeReceiptAsync((id,rv)=>Purchasing.CancelPurchaseReceiptAsync(id,new("إلغاء من واجهة المشتريات",rv)));
    }
    private async Task ChangeReceiptAsync(Func<Guid,string,Task<PurchaseReceiptDto?>> action)
    {
        if(ActiveTab?.Model is not UiPurchaseReceiptEditorModel m||m.Id is not Guid id)return;
        try{var dto=await action(id,m.RowVersion??string.Empty);if(dto is not null)SetActive(Map(dto));}
        catch(ApiClientException ex)
        {
            if (ex.Error.Code == "purchasing_receipt_value_invalid")
            {
                _purchaseReceiptEditor?.ShowPostingValueError();
                Snackbar.Warning("قيمة الاستلام صفر: صحح التكلفة الفعلية في المسودة قبل التأكيد.");
            }
            else ApiFeedback.Show(ex.Error);
        }
        catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task PostReceiptAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseReceiptEditorModel m||m.Id is not Guid id)return;
        if (_purchaseReceiptEditor is not null && !_purchaseReceiptEditor.ValidateForPosting())
        {
            Snackbar.Warning("قيمة الاستلام صفر: أعد السند إلى المسودة وصحح تكلفة البند المحدد بالأحمر ثم احفظه وأكده.");
            return;
        }
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

            // Validate the period before confirmation so a missing/locked period is
            // explained at the posting-date field instead of surfacing as a thrown conflict.
            // The transaction's server-side validation remains mandatory.
            if (!await CheckReceiptPostingPeriodAsync(m)) return;

            if(!await Dialog.ConfirmAsync("ترحيل الاستلام","سيتم تحديث المخزون وإنشاء قيد Inventory / GRNI داخل عملية واحدة.",AlertTone.Warning,"ترحيل","رجوع"))return;
            await Purchasing.PostPurchaseReceiptAsync(id,new(m.RowVersion??string.Empty));
            var dto=await Purchasing.GetPurchaseReceiptAsync(id);
            if(dto is not null)SetActive(Map(dto));
            Snackbar.Success("تم ترحيل الاستلام وتحديث المخزون والمحاسبة.");
        }
        catch (ApiClientException ex)
        {
            // A period may have changed between the UI check and the transaction.
            var error = PurchaseReceiptPostingPeriodValidation.Explain(ex.Error.Code, m.PostingDate);
            if (error is not null)
            {
                m.PostingDateError = error;
                Snackbar.Warning(error);
            }
            else if (ex.Error.Code == "purchasing_receipt_value_invalid")
            {
                _purchaseReceiptEditor?.ShowPostingValueError();
                Snackbar.Warning("قيمة الاستلام صفر: أعد السند إلى المسودة وعدل التكلفة الفعلية، ثم احفظ وأكد السند قبل ترحيله.");
            }
            else ApiFeedback.Show(ex.Error);
        }
        catch { ApiFeedback.ShowUnexpected(); }
    }

    private async Task<bool> CheckReceiptPostingPeriodAsync(UiPurchaseReceiptEditorModel receipt)
    {
        receipt.PostingDateError = null;
        var periods = new List<FiscalPeriodDto>();
        try
        {
            // Read every page: checking the first 20 periods can produce false
            // 'not found' results for older dates or businesses with many years.
            for (var pageNumber = 1; ; pageNumber++)
            {
                var page = await Accounting.GetFiscalPeriodsPageAsync(
                    new PageRequest { PageNumber = pageNumber, PageSize = PageRequest.MaximumPageSize });
                periods.AddRange(page.Items);
                if (pageNumber >= page.TotalPages || page.Items.Count == 0) break;
            }
        }
        catch (ApiClientException)
        {
            // Period-list permissions may differ from purchase-posting permissions.
            // Never deny a valid posting just because the client cannot read the list.
            // The authoritative server validation still runs within the transaction.
            return true;
        }

        var error = PurchaseReceiptPostingPeriodValidation.Validate(receipt.PostingDate, periods);
        if (error is null) return true;

        receipt.PostingDateError = error;
        Snackbar.Warning(error);
        return false;
    }

    private async Task CreateInvoiceFromReceiptAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseReceiptEditorModel current ||
           current.Id is not Guid receiptId ||
           current.Status!=(byte)PurchaseReceiptStatus.Posted) return;
        try
        {
            var receipt=await Purchasing.GetPurchaseReceiptAsync(receiptId);
            if(receipt is null){Snackbar.Warning("تعذر تحميل سند الاستلام.");return;}
            var model=await PrepareInvoiceFromReceiptAsync(receipt);
            if(model is null)return;
            var tab=Workspace.OpenNew(PurchasingEntityType.PurchaseInvoices,$"فاتورة من {receipt.ReceiptCode}");
            tab.Model=model;tab.IsDirty=true;tab.IsEditMode=true;
            Navigation.NavigateTo("/purchases/invoices");
            Snackbar.Success("تم جلب بيانات الفاتورة من الاستلام. راجع رقم فاتورة المورد واحفظ؛ ستتم محاولة المطابقة تلقائيًا.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }

    private async Task InvoiceReceiptChangedAsync(Guid? receiptId)
    {
        var tab=ActiveTab;
        if(tab?.Model is not UiPurchaseInvoiceEditorModel invoice || invoice.Id.HasValue || !IsEditable)return;
        if(!receiptId.HasValue)
        {
            var blank=NewInvoice();
            blank.SupplierInvoiceCode=invoice.SupplierInvoiceCode;
            tab.Model=blank;tab.Title="فاتورة مورد جديدة";tab.IsDirty=true;
            return;
        }
        try
        {
            var receipt=await Purchasing.GetPurchaseReceiptAsync(receiptId.Value);
            if(receipt is null){Snackbar.Warning("لم يُعثر على سند الاستلام.");return;}
            var prepared=await PrepareInvoiceFromReceiptAsync(receipt);
            if(prepared is null)
            {
                invoice.SourceReceiptError="لا توجد كمية غير مفوترة متاحة في هذا الاستلام. اختر استلامًا آخر أو راجع الفواتير المرتبطة به.";
                return;
            }
            // Keep fields the user may have typed before selecting the receipt.
            prepared.SupplierInvoiceCode=invoice.SupplierInvoiceCode;
            prepared.InvoiceDate=invoice.InvoiceDate;
            prepared.PostingDate=invoice.PostingDate;
            tab.Model=prepared;tab.Title=$"فاتورة من {receipt.ReceiptCode}";tab.IsDirty=true;
            Snackbar.Success("تمت تعبئة المورد والعملة والأصناف والكميات والأسعار تلقائيًا من سند الاستلام.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }

    private async Task RefreshSelectedInvoiceReceiptAsync()
    {
        if(ActiveTab?.Model is not UiPurchaseInvoiceEditorModel invoice ||
           invoice.Id.HasValue || !invoice.SourceReceiptId.HasValue || !IsEditable)return;
        if(!await Dialog.ConfirmAsync("تحديث كميات الاستلام",
            "سيُعاد جلب الأصناف والكميات المتاحة من الاستلام، وستُستبدل التعديلات غير المحفوظة على الكميات والأسعار. هل تريد المتابعة؟",
            AlertTone.Warning,"تحديث","إلغاء"))return;
        await InvoiceReceiptChangedAsync(invoice.SourceReceiptId.Value);
    }

    private async Task<UiPurchaseInvoiceEditorModel?> PrepareInvoiceFromReceiptAsync(PurchaseReceiptDto receipt)
    {
        if(receipt.Status!=PurchaseReceiptStatus.Posted)
        {
            Snackbar.Warning("يجب ترحيل سند الاستلام قبل إعداد فاتورة المورد.");
            return null;
        }
        var purchaseOrderId=receipt.PurchaseOrderId;
        var order=await Purchasing.GetPurchaseOrderAsync(purchaseOrderId);
        if(order is null)
        {
            Snackbar.Warning("تعذر تحميل أمر الشراء المرتبط بسند الاستلام.");
            return null;
        }
        if(order.SupplierId!=receipt.SupplierId)
        {
            Snackbar.Warning("مورد سند الاستلام لا يطابق مورد أمر الشراء.");
            return null;
        }

        // Do not offer quantities that have already been matched to another invoice.
        var availability=await Purchasing.GetPurchaseReceiptInvoiceAvailabilityAsync(receipt.Id);
        var remainingByLine=(availability??[]).ToDictionary(x=>x.ReceiptLineId,x=>x.AvailableQuantity);
        var orderLines=order.Lines.ToDictionary(x=>x.Id);
        var model=new UiPurchaseInvoiceEditorModel
        {
            SupplierId=order.SupplierId,
            SupplierName=order.SupplierName,
            InvoiceDate=DateOnly.FromDateTime(DateTime.Today),
            PostingDate=DateOnly.FromDateTime(DateTime.Today),
            CurrencyId=order.CurrencyId,
            CurrencyCode=order.CurrencyCode,
            ExchangeRate=order.ExchangeRate,
            ExchangeRateDate=order.ExchangeRateDate,
            TaxCalculationMode=(byte)order.TaxCalculationMode,
            Notes=$"فاتورة مورد من الاستلام {receipt.ReceiptCode} / أمر الشراء {order.PurchaseOrderCode}"
        };

        foreach(var receiptLine in receipt.Lines.Where(x=>remainingByLine.GetValueOrDefault(x.Id)>0m).OrderBy(x=>x.LineSequence))
        {
            if(!orderLines.TryGetValue(receiptLine.PurchaseOrderLineId,out var poLine))
            {
                Snackbar.Warning($"تعذر العثور على سطر أمر الشراء المرتبط ببند الاستلام {receiptLine.LineSequence}.");
                return null;
            }

            var quantity=remainingByLine[receiptLine.Id];
            if(quantity<=0m)continue;
            var ratio=poLine.OrderedQuantity<=0m?0m:quantity/poLine.OrderedQuantity;
            var discount=Math.Round(poLine.DiscountAmount*ratio,4);
            var gross=Math.Round(quantity*poLine.UnitPrice,4);
            var net=Math.Round(poLine.NetAmount*ratio,4);
            var tax=Math.Round(poLine.TaxAmount*ratio,4);
            var final=Math.Round(poLine.FinalAmount*ratio,4);

            model.Lines.Add(new UiPurchaseInvoiceLineModel
            {
                LineSequence=model.Lines.Count+1,
                PurchaseOrderLineId=poLine.Id,
                ProductVariantId=poLine.ProductVariantId,
                ProductName=poLine.ProductNameSnapshot,
                ProductCode=receiptLine.ProductCode ?? poLine.ProductCodeSnapshot,
                SourceReceiptLineId=receiptLine.Id,
                SourceReceiptLineSequence=receiptLine.LineSequence,
                AvailableToInvoiceQuantity=quantity,
                Quantity=quantity,
                UnitPrice=poLine.UnitPrice,
                GrossAmount=gross,
                DiscountAmount=discount,
                NetAmount=net,
                TaxRate=poLine.TaxRate,
                TaxAmount=tax,
                FinalAmount=final
            });
        }

        if(model.Lines.Count==0)
        {
            Snackbar.Warning("جميع كميات هذا الاستلام تمت فوترتها أو إرجاعها. اختر استلامًا آخر يحتوي كميات غير مفوترة.");
            return null;
        }

        model.Subtotal=Math.Round(model.Lines.Sum(x=>x.GrossAmount),4);
        model.DiscountAmount=Math.Round(model.Lines.Sum(x=>x.DiscountAmount),4);
        model.TaxAmount=Math.Round(model.Lines.Sum(x=>x.TaxAmount),4);
        model.TotalAmount=Math.Round(model.Lines.Sum(x=>x.FinalAmount),4);

        model.SourceReceiptId=receipt.Id;
        model.SourceReceiptCode=receipt.ReceiptCode;
        return model;
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
        try
        {
            var dto=await action(id,m.RowVersion??string.Empty);
            if(dto is not null)
            {
                var updated=Map(dto);
                SetActive(updated);
                await LoadInvoiceMatchingReadinessAsync(updated);
            }
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task MatchInvoiceAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseInvoiceEditorModel m||m.Id is not Guid id||m.SupplierId is not Guid supplier)return;
        m.MatchingNotice=null;
        ClearInvoiceQuantityErrors(m);
        try
        {
            // When an invoice was prepared from a specific posted receipt, match only
            // against that receipt. For invoices reopened later, search ALL receipt pages:
            // using just the latest 100 can incorrectly report zero available for an older PO.
            var receiptPool=new List<PurchaseReceiptDto>();
            if(m.SourceReceiptId is Guid selectedReceiptId)
            {
                var selectedReceipt=await Purchasing.GetPurchaseReceiptAsync(selectedReceiptId);
                if(selectedReceipt is not null && selectedReceipt.Status==PurchaseReceiptStatus.Posted &&
                    selectedReceipt.SupplierId==supplier)
                    receiptPool.Add(selectedReceipt);
            }
            else
            {
                var receiptPageNumber=1;
                var totalReceiptPages=1;
                while(receiptPageNumber<=totalReceiptPages)
                {
                    var page=await Purchasing.GetPurchaseReceiptsAsync(
                        new PageRequest{PageNumber=receiptPageNumber,PageSize=100,
                            SortBy="PostingDate",SortDirection=SortDirection.Descending},
                        PurchaseReceiptStatus.Posted,null,supplier);
                    receiptPool.AddRange(page.Items);
                    totalReceiptPages=page.TotalPages;
                    receiptPageNumber++;
                }
            }
            var lines=m.Lines.Where(x=>x.Id.HasValue&&x.ProductVariantId.HasValue&&x.PurchaseOrderLineId.HasValue).ToArray();
            if(lines.Length==0||lines.Length!=m.Lines.Count)
            {
                m.MatchingNotice="يجب ربط جميع أصناف الفاتورة ببنود أوامر الشراء قبل تشغيل المطابقة.";
                Snackbar.Warning(m.MatchingNotice);return;
            }
            var receipts=receiptPool.SelectMany(x=>x.Lines)
                .Where(x=>lines.Any(l=>l.ProductVariantId==x.ProductVariantId&&l.PurchaseOrderLineId==x.PurchaseOrderLineId))
                .ToArray();
            if(receipts.Length==0)
            {
                m.MatchingNotice="لا توجد استلامات مرحلة مناسبة لأصناف الفاتورة. تأكد من استلام البضاعة وترحيل سندها أولًا.";
                Snackbar.Warning(m.MatchingNotice);return;
            }

            // The receipt DTO's AcceptedQuantity does not deduct quantities already matched
            // to other invoices. Ask the server for its actual remaining quantity first.
            var availability=await Purchasing.GetPurchaseInvoiceMatchAvailabilityAsync(id,receipts.Select(x=>x.Id).Distinct().ToArray());
            var remaining=(availability??[]).ToDictionary(x=>x.ReceiptLineId,x=>x.AvailableQuantity);
            var candidates=receipts.Where(x=>remaining.ContainsKey(x.Id))
                .Select(x=>new ReceiptAvailableLine(x.Id,x.PurchaseOrderLineId,x.ProductVariantId,remaining[x.Id],0m))
                .ToArray();
            var demands=lines.Select(x=>new InvoiceReceiptDemand(x.Id!.Value,x.PurchaseOrderLineId,
                x.ProductVariantId,x.Quantity,x.LineSequence)).ToArray();
            if(!PurchaseInvoiceReceiptAllocationPlanner.TryPlanDetailed(demands,candidates,
                out var plan,out var issue,out var error))
            {
                ShowInvoiceQuantityIssue(m,issue,error);
                if(issue is null) m.MatchingNotice=error;
                Snackbar.Warning("راجع الكمية المحددة بالأحمر أو اختر استلامًا آخر متاحًا للمطابقة.");
                return;
            }
            var allocations=plan.Select(x=>new PurchaseInvoiceMatchAllocationRequest(
                x.InvoiceLineId,x.ReceiptLineId,x.Quantity)).ToArray();
            var result=await Purchasing.MatchPurchaseInvoiceAsync(id,new(allocations,m.RowVersion??string.Empty));
            if(result is null)return;
            var refreshed=await Purchasing.GetPurchaseInvoiceAsync(id);
            if(refreshed is null)return;
            var mapped=Map(refreshed);
            mapped.Match=Map(result);
            mapped.MatchingNotice=result.RequiresApproval
                ? "تمت المطابقة. توجد فروقات تحتاج إلى المراجعة والاعتماد قبل الترحيل."
                : null;
            SetActive(mapped);
            await LoadInvoiceMatchingReadinessAsync(mapped);
            Snackbar.Success("تم تشغيل المطابقة الثلاثية. راجع الفروقات قبل ترحيل الفاتورة.");
        }
        catch(ApiClientException ex)
        {
            if(ex.Error.Code is "purchasing_match_quantity_exceeds_available_receipt" or "purchasing_match_quantity_exceeds_invoice" or "purchasing_invoice_line_match_required")
            {
                var marked=m.SourceReceiptId.HasValue &&
                    await RefreshInvoiceQuantityIssueAsync(m,m.SourceReceiptId.Value,id);
                if(!marked)m.MatchingNotice=PurchaseInvoiceMatchingMessages.FromApiError(ex.Error);
                Snackbar.Warning(marked
                    ? "راجع الكمية المحددة بالأحمر في بنود الفاتورة."
                    : m.MatchingNotice!);
            }
            else ApiFeedback.Show(ex.Error);
        }
        catch{ApiFeedback.ShowUnexpected();}
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
            var refreshed=await Purchasing.GetPurchaseInvoiceAsync(id);
            if(refreshed is not null)
            {
                var mapped=Map(refreshed);
                SetActive(mapped);
                await LoadInvoiceMatchingReadinessAsync(mapped);
            }
            Snackbar.Success("تم اعتماد الفروقات.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
    private async Task PostInvoiceAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseInvoiceEditorModel m||m.Id is not Guid id)return;
        try
        {
            // Read the persisted allocations, not the in-memory Match panel. A re-opened
            // invoice can be fully matched even though that panel has not been populated.
            var readiness=await Purchasing.GetPurchaseInvoiceMatchingReadinessAsync(id);
            if(readiness is null)
            {
                Snackbar.Warning("تعذر التحقق من مطابقة الفاتورة. حدّث الصفحة ثم أعد المحاولة.");
                return;
            }
            if(!readiness.IsComplete)
            {
                m.MatchingNotice=readiness.Message;
                Snackbar.Warning(readiness.Message);
                await InvokeAsync(StateHasChanged);
                return;
            }
            m.MatchingNotice=null;
            if(!await Dialog.ConfirmAsync("ترحيل فاتورة المورد",
                "المطابقة مكتملة. سيتم إنشاء قيد GRNI / الضريبة / فروقات السعر / حساب المورد.",
                AlertTone.Warning,"ترحيل","رجوع"))return;

            await Purchasing.PostPurchaseInvoiceAsync(id,new(m.RowVersion??string.Empty));
            var dto=await Purchasing.GetPurchaseInvoiceAsync(id);
            if(dto is not null)
            {
                var mapped=Map(dto);
                SetActive(mapped);
                await LoadInvoicePaymentSummaryAsync(mapped,true);
            }
            Snackbar.Success("تم ترحيل فاتورة المورد.");
        }
        catch(ApiClientException ex) when(ex.Error.Code is
            "purchasing_invoice_matching_required" or "purchasing_invoice_matching_incomplete" or
            "purchasing_invoice_variance_approval_required")
        {
            // The server remains authoritative if allocations change after the pre-check.
            m.MatchingNotice=ex.Error.Code switch
            {
                "purchasing_invoice_matching_required" => "لم تتم مطابقة الفاتورة. اضغط «تشغيل المطابقة» أولًا، ثم راجع النتيجة.",
                "purchasing_invoice_variance_approval_required" => "توجد فروقات مطابقة تحتاج إلى اعتماد قبل الترحيل.",
                _ => "المطابقة غير مكتملة. راجع بنود المطابقة واعتمد الفروقات المعلقة قبل الترحيل."
            };
            Snackbar.Warning(m.MatchingNotice);
            await InvokeAsync(StateHasChanged);
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }

    private async Task PayInvoiceAsync(MouseEventArgs _)
    {
        if(ActiveTab?.Model is not UiPurchaseInvoiceEditorModel m||m.Id is not Guid id||m.CurrencyId is not Guid currencyId)return;
        try
        {
            var summary=await Purchasing.GetPurchaseInvoicePaymentSummaryAsync(id);
            if(summary is null)return;
            _invoicePaymentSummaries[id]=summary;
            StateHasChanged();
            if(summary.OutstandingAmount<=0m)
            {
                Snackbar.Info("فاتورة المورد مسددة بالكامل بالفعل.");
                return;
            }

            var result=await Dialog.ShowAsync<UiPurchaseInvoicePaymentDialog>(
                "سداد فاتورة المورد",
                new Dictionary<string,object>
                {
                    [nameof(UiPurchaseInvoicePaymentDialog.InvoiceCode)]=m.PurchaseInvoiceCode,
                    [nameof(UiPurchaseInvoicePaymentDialog.SupplierName)]=m.SupplierName??"المورد",
                    [nameof(UiPurchaseInvoicePaymentDialog.CurrencyCode)]=m.CurrencyCode??string.Empty,
                    [nameof(UiPurchaseInvoicePaymentDialog.TotalAmount)]=summary.TotalAmount,
                    [nameof(UiPurchaseInvoicePaymentDialog.PaidAmount)]=summary.PaidAmount,
                    [nameof(UiPurchaseInvoicePaymentDialog.OutstandingAmount)]=summary.OutstandingAmount,
                    [nameof(UiPurchaseInvoicePaymentDialog.PaymentDate)]=DateOnly.FromDateTime(DateTime.Today),
                    [nameof(UiPurchaseInvoicePaymentDialog.CashAccountSearchAsync)]=new Func<string,CancellationToken,Task<IReadOnlyList<UiLookupItem>>>((text,ct)=>SearchInvoiceCashAccountsAsync(currencyId,text,ct)),
                    [nameof(UiPurchaseInvoicePaymentDialog.BankAccountSearchAsync)]=new Func<string,CancellationToken,Task<IReadOnlyList<UiLookupItem>>>((text,ct)=>SearchInvoiceBankAccountsAsync(currencyId,text,ct))
                },
                new UiDialogOptions{Size=UiDialogSize.Large,CloseOnBackdrop=false});

            if(result.Cancelled||result.Value is not UiPurchaseInvoicePaymentDialog.PaymentRequestModel payment)return;

            var response=await Purchasing.PayPurchaseInvoiceAsync(id,new PayPurchaseInvoiceRequest(
                payment.PaymentDate,
                payment.Amount,
                (OAS.Contracts.Accounting.Enums.PaymentMethod)(byte)payment.PaymentMethod,
                payment.CashAccountId,
                payment.BankAccountId,
                payment.ReferenceNumber,
                $"سداد فاتورة المورد {m.PurchaseInvoiceCode}"));
            if(response is null)return;
            await LoadInvoicePaymentSummaryAsync(m,true);

            Snackbar.Success(response.OutstandingAmount<=0m
                ? $"تم سداد الفاتورة بالكامل وإنشاء سند الصرف {response.PaymentVoucherNumber}."
                : $"تم تسجيل السداد وإنشاء سند الصرف {response.PaymentVoucherNumber}. المتبقي {response.OutstandingAmount:#,##0.##} {m.CurrencyCode}.");
        }
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

    private async Task PrintPurchaseInvoiceAsync(MouseEventArgs _)
    {
        if(_isPrintingPurchaseInvoice||ActiveTab?.Model is not UiPurchaseInvoiceEditorModel invoice||invoice.Id is not Guid id)return;
        if(invoice.Status!=(byte)PurchaseInvoiceStatus.Posted)
        {
            Snackbar.Info("يمكن طباعة فاتورة المشتريات بعد الترحيل فقط.");
            return;
        }
        if(ActiveTab.IsDirty)
        {
            Snackbar.Warning("احفظ التعديلات قبل إرسال الفاتورة للطباعة.");
            return;
        }

        _isPrintingPurchaseInvoice=true;
        try
        {
            var result=await Printing.PrintPurchaseInvoiceAsync(id);
            if(result is null)Snackbar.Error("تعذر إرسال فاتورة المشتريات إلى OAS Print.");
            else Snackbar.Success("تم إرسال فاتورة المشتريات إلى OAS Print للطباعة.");
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}
        catch{ApiFeedback.ShowUnexpected();}
        finally
        {
            _isPrintingPurchaseInvoice=false;
            await InvokeAsync(StateHasChanged);
        }
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
