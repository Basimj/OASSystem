using OAS.Client.Services.Http;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Enums;
using OAS.UiLib.Core.Models;
using OAS.UiLib.Core.Models.Purchasing;

namespace OAS.Client.Purchasing.Components;

public partial class PurchasingWorkspaceHost
{
    private async Task<IReadOnlyList<UiLookupItem>> SearchSuppliersAsync(string search,CancellationToken ct)
    {
        var items=await Accounting.LookupSuppliersAsync(string.IsNullOrWhiteSpace(search)?null:search,ct)??[];
        return items.Where(x=>x.IsActive).Take(12).Select(x=>new UiLookupItem(x.Id.ToString("D"),x.NameAr,$"{x.SupplierCode} • {x.AccountCode}","fa-solid fa-truck-field")).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchWarehousesAsync(string search,CancellationToken ct)
    {
        var page=await Inventory.GetWarehousesAsync(LookupRequest(search),ct);
        return page.Items.Where(x=>x.IsActive).Select(x=>new UiLookupItem(x.Id.ToString("D"),x.NameAr,x.Code,"fa-solid fa-warehouse")).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchProductsAsync(string search,CancellationToken ct)
    {
        var page=await Inventory.GetProductVariantsAsync(LookupRequest(search),ct);
        return page.Items.Where(x=>x.IsActive).Select(x=>new UiLookupItem(x.Id.ToString("D"),string.IsNullOrWhiteSpace(x.VariantName)?x.SKU:x.VariantName,x.SKU,"fa-solid fa-box")).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchUnitsAsync(string search,CancellationToken ct)
    {
        var page=await Inventory.GetUnitsAsync(LookupRequest(search),ct);
        return page.Items.Where(x=>x.IsActive).Select(x=>new UiLookupItem(x.Id.ToString("D"),x.NameAr,x.Code,"fa-solid fa-ruler")).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchCurrenciesAsync(string search,CancellationToken ct)
    {
        var page=await Accounting.GetCurrenciesPageAsync(LookupRequest(search),ct);
        return page.Items.Where(x=>x.IsActive).Select(x=>new UiLookupItem(x.Id.ToString("D"),$"{x.Code} - {x.NameAr}",x.Symbol,"fa-solid fa-coins")).ToArray();
    }
    private async Task<IReadOnlyList<UiLookupItem>> SearchReceivableOrdersAsync(string search,CancellationToken ct)
    {
        var request=LookupRequest(search);
        var sent=await Purchasing.GetPurchaseOrdersAsync(request,PurchaseOrderStatus.Sent,cancellationToken:ct);
        var partial=await Purchasing.GetPurchaseOrdersAsync(request,PurchaseOrderStatus.PartiallyReceived,cancellationToken:ct);
        return sent.Items.Concat(partial.Items).GroupBy(x=>x.Id).Select(x=>x.First()).Take(15).Select(x=>new UiLookupItem(x.Id.ToString("D"),x.PurchaseOrderCode,$"{x.SupplierName} • {x.DestinationWarehouseName}","fa-solid fa-file-signature")).ToArray();
    }
    private static PageRequest LookupRequest(string search)=>new(){PageNumber=1,PageSize=12,Search=string.IsNullOrWhiteSpace(search)?null:search.Trim(),SortDirection=SortDirection.Ascending};

    private async Task ReceiptPurchaseOrderChangedAsync(Guid? purchaseOrderId)
    {
        if(ActiveTab?.Model is not UiPurchaseReceiptEditorModel m)return;
        m.Lines.Clear();m.SupplierId=null;m.SupplierName=null;m.WarehouseId=null;m.WarehouseName=null;m.PurchaseOrderCode=null;
        if(purchaseOrderId is not Guid id)return;
        try
        {
            var po=await Purchasing.GetPurchaseOrderAsync(id);if(po is null)return;
            if(po.Status is not (PurchaseOrderStatus.Sent or PurchaseOrderStatus.PartiallyReceived)){Snackbar.Warning("أمر الشراء المحدد غير متاح للاستلام.");return;}
            m.PurchaseOrderId=po.Id;m.PurchaseOrderCode=po.PurchaseOrderCode;m.SupplierId=po.SupplierId;m.SupplierName=po.SupplierName;m.WarehouseId=po.DestinationWarehouseId;m.WarehouseName=po.DestinationWarehouseName;
            foreach(var line in po.Lines.Where(x=>x.RemainingBaseQuantity>0))
            {
                var factor=line.UnitConversionFactor<=0?1m:line.UnitConversionFactor;var remaining=Math.Round(line.RemainingBaseQuantity/factor,3);
                m.Lines.Add(new(){PurchaseOrderLineId=line.Id,LineSequence=m.Lines.Count+1,ProductVariantId=line.ProductVariantId,ProductName=line.ProductNameSnapshot,OrderedQuantitySnapshot=line.OrderedQuantity,PreviouslyReceivedQty=Math.Max(0,line.OrderedQuantity-remaining),RemainingReceivableQuantity=remaining,ActualUnitCost=CalculateBaseReceiptUnitCost(line, po.ExchangeRate)});
            }
            if(ActiveTab is { } tab)tab.IsDirty=true;StateHasChanged();
        }
        catch(ApiClientException ex){ApiFeedback.Show(ex.Error);}catch{ApiFeedback.ShowUnexpected();}
    }
}
