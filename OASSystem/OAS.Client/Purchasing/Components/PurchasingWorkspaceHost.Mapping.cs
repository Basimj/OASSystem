using OAS.Client.Purchasing.Common;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Contracts.Purchasing.PurchaseRequests;
using OAS.Contracts.Purchasing.SupplierCatalog;
using OAS.UiLib.Core.Models.Purchasing;

namespace OAS.Client.Purchasing.Components;

public partial class PurchasingWorkspaceHost
{
    private static UiSupplierCatalogEditorModel Map(SupplierCatalogItemDto x)
    {
        var m=new UiSupplierCatalogEditorModel{Id=x.Id,SupplierId=x.SupplierId,SupplierName=x.SupplierName,ProductVariantId=x.ProductVariantId,ProductName=x.ProductName,SupplierProductCode=x.SupplierProductCode,SupplierProductName=x.SupplierProductName,PurchaseUnitId=x.PurchaseUnitId,PurchaseUnitName=x.PurchaseUnitName,UnitConversionFactor=x.UnitConversionFactor,LeadTimeDays=x.LeadTimeDays,MinimumOrderQuantity=x.MinimumOrderQuantity,IsPreferred=x.IsPreferred,IsActive=x.IsActive,RowVersion=x.RowVersion};
        m.PriceHistory.AddRange(x.PriceHistory.Select(p=>new UiSupplierPriceRowModel{Id=p.Id,CurrencyCode=p.CurrencyCode??string.Empty,UnitPrice=p.UnitPrice,EffectiveFrom=p.EffectiveFrom,EffectiveTo=p.EffectiveTo,IsCurrent=p.IsCurrent,Notes=p.Notes}));
        return m;
    }
    private static UiPurchaseRequestEditorModel Map(PurchaseRequestDto x)
    {
        var m=new UiPurchaseRequestEditorModel{Id=x.Id,RequestCode=x.RequestCode,RequestType=(byte)x.RequestType,Status=(byte)x.Status,StatusText=PurchasingArabicPresenter.RequestStatus(x.Status),WarehouseId=x.WarehouseId,WarehouseName=x.WarehouseName,CustomerOrderId=x.CustomerOrderId,RequestDate=x.RequestDate,RequiredDate=x.RequiredDate,Reason=x.Reason,Notes=x.Notes,RowVersion=x.RowVersion};
        m.Lines.AddRange(x.Lines.Select(l=>new UiPurchaseRequestLineModel{Id=l.Id,LineSequence=l.LineSequence,ProductVariantId=l.ProductVariantId,ProductName=l.ProductName,RequestedQuantity=l.RequestedQuantity,AllocatedQuantity=l.AllocatedToPurchaseOrderQuantity,RemainingQuantity=l.RemainingQuantity,RequiredDate=l.RequiredDate,PreferredSupplierId=l.PreferredSupplierId,PreferredSupplierName=l.PreferredSupplierName,Notes=l.Notes,RowVersion=l.RowVersion})); return m;
    }
    private static UiPurchaseOrderEditorModel Map(PurchaseOrderDto x)
    {
        var m=new UiPurchaseOrderEditorModel{Id=x.Id,PurchaseOrderCode=x.PurchaseOrderCode,Status=(byte)x.Status,StatusText=PurchasingArabicPresenter.OrderStatus(x.Status),SupplierId=x.SupplierId,SupplierName=x.SupplierName,DestinationWarehouseId=x.DestinationWarehouseId,DestinationWarehouseName=x.DestinationWarehouseName,OrderDate=x.OrderDate,ExpectedDeliveryDate=x.ExpectedDeliveryDate,CurrencyId=x.CurrencyId,CurrencyCode=x.CurrencyCode,ExchangeRate=x.ExchangeRate,ExchangeRateDate=x.ExchangeRateDate,TaxCalculationMode=(byte)x.TaxCalculationMode,PaymentTermDays=x.PaymentTermDays,Subtotal=x.Subtotal,DiscountAmount=x.DiscountAmount,TaxAmount=x.TaxAmount,TotalAmount=x.TotalAmount,Notes=x.Notes,RowVersion=x.RowVersion};
        foreach(var l in x.Lines){var lm=new UiPurchaseOrderLineModel{Id=l.Id,LineSequence=l.LineSequence,ProductVariantId=l.ProductVariantId,ProductName=l.ProductNameSnapshot,SupplierCatalogItemId=l.SupplierCatalogItemId,PurchaseUnitId=l.PurchaseUnitId,PurchaseUnitName=l.UnitNameSnapshot,UnitConversionFactor=l.UnitConversionFactor,OrderedQuantity=l.OrderedQuantity,BaseQuantity=l.BaseQuantity,ReceivedBaseQuantity=l.ReceivedBaseQuantity,RemainingBaseQuantity=l.RemainingBaseQuantity,UnitPrice=l.UnitPrice,DiscountAmount=l.DiscountAmount,TaxRate=l.TaxRate,TaxAmount=l.TaxAmount,FinalAmount=l.FinalAmount,ExpectedDeliveryDate=l.ExpectedDeliveryDate,Notes=l.Notes,RowVersion=l.RowVersion};lm.Sources.AddRange(l.Sources.Select(s=>new UiPurchaseOrderLineSourceModel{Id=s.Id,PurchaseRequestLineId=s.PurchaseRequestLineId,AllocatedQuantity=s.AllocatedQuantity,RowVersion=s.RowVersion}));m.Lines.Add(lm);} return m;
    }
    private static UiPurchaseReceiptEditorModel Map(PurchaseReceiptDto x)
    {
        var m=new UiPurchaseReceiptEditorModel{Id=x.Id,ReceiptCode=x.ReceiptCode,Status=(byte)x.Status,StatusText=PurchasingArabicPresenter.ReceiptStatus(x.Status),PurchaseOrderId=x.PurchaseOrderId,PurchaseOrderCode=x.PurchaseOrderCode,SupplierId=x.SupplierId,SupplierName=x.SupplierName,WarehouseId=x.WarehouseId,WarehouseName=x.WarehouseName,ReceiptDate=x.ReceiptDate,PostingDate=x.PostingDate,SupplierDeliveryCode=x.SupplierDeliveryCode,InventoryTransactionId=x.InventoryTransactionId,JournalEntryId=x.JournalEntryId,Notes=x.Notes,RowVersion=x.RowVersion};
        m.Lines.AddRange(x.Lines.Select(l=>new UiPurchaseReceiptLineModel{Id=l.Id,PurchaseOrderLineId=l.PurchaseOrderLineId,LineSequence=l.LineSequence,ProductVariantId=l.ProductVariantId,ProductName=l.ProductName,OrderedQuantitySnapshot=l.OrderedQuantitySnapshot,PreviouslyReceivedQty=l.PreviouslyReceivedQty,RemainingReceivableQuantity=l.RemainingReceivableQuantity,ReceivedQuantity=l.ReceivedQuantity,AcceptedQuantity=l.AcceptedQuantity,RejectedQuantity=l.RejectedQuantity,BaseAcceptedQuantity=l.BaseAcceptedQuantity,ActualUnitCost=l.ActualUnitCost,TotalAcceptedCost=l.TotalAcceptedCost,ExpiryDate=l.ExpiryDate,BatchCode=l.BatchCode,Notes=l.Notes,RowVersion=l.RowVersion})); return m;
    }
    private static UiPurchaseInvoiceEditorModel Map(PurchaseInvoiceDto x)
    {
        var m=new UiPurchaseInvoiceEditorModel{Id=x.Id,PurchaseInvoiceCode=x.PurchaseInvoiceCode,SupplierInvoiceCode=x.SupplierInvoiceCode,Status=(byte)x.Status,StatusText=PurchasingArabicPresenter.InvoiceStatus(x.Status),SupplierId=x.SupplierId,SupplierName=x.SupplierName,InvoiceDate=x.InvoiceDate,PostingDate=x.PostingDate,CurrencyId=x.CurrencyId,CurrencyCode=x.CurrencyCode,ExchangeRate=x.ExchangeRate,ExchangeRateDate=x.ExchangeRateDate,TaxCalculationMode=(byte)x.TaxCalculationMode,Subtotal=x.Subtotal,DiscountAmount=x.DiscountAmount,TaxAmount=x.TaxAmount,TotalAmount=x.TotalAmount,JournalEntryId=x.JournalEntryId,Notes=x.Notes,RowVersion=x.RowVersion};
        m.Lines.AddRange(x.Lines.Select(l=>new UiPurchaseInvoiceLineModel{Id=l.Id,LineSequence=l.LineSequence,PurchaseOrderLineId=l.PurchaseOrderLineId,ProductVariantId=l.ProductVariantId,ProductName=l.DescriptionSnapshot,Quantity=l.Quantity,UnitPrice=l.UnitPrice,GrossAmount=l.GrossAmount,DiscountAmount=l.DiscountAmount,NetAmount=l.NetAmount,TaxRate=l.TaxRate,TaxAmount=l.TaxAmount,FinalAmount=l.FinalAmount,RowVersion=l.RowVersion})); return m;
    }
    private static UiPurchaseMatchModel Map(PurchaseMatchResultDto x)
    {
        var m=new UiPurchaseMatchModel{OverallStatus=PurchasingArabicPresenter.MatchStatus(x.OverallStatus),RequiresApproval=x.RequiresApproval};
        m.Allocations.AddRange(x.Allocations.Select(a=>new UiPurchaseMatchAllocationModel{Id=a.Id,PurchaseInvoiceLineId=a.PurchaseInvoiceLineId,PurchaseReceiptLineId=a.PurchaseReceiptLineId,MatchedQuantity=a.MatchedQuantity,MatchedNetAmount=a.MatchedNetAmount,QuantityVariance=a.QuantityVariance,PriceVarianceAmount=a.PriceVarianceAmount,TaxVarianceAmount=a.TaxVarianceAmount,Status=PurchasingArabicPresenter.MatchStatus(a.MatchStatus),RowVersion=a.RowVersion}));
        m.Variances.AddRange(x.Variances.Select(v=>new UiPurchaseVarianceModel{Type=PurchasingArabicPresenter.VarianceType(v.Type),PurchaseInvoiceLineId=v.PurchaseInvoiceLineId,PurchaseReceiptLineId=v.PurchaseReceiptLineId,ExpectedValue=v.ExpectedValue,ActualValue=v.ActualValue,Variance=v.Variance,Tolerance=v.Tolerance,Status=PurchasingArabicPresenter.MatchStatus(v.Status),Message=v.Message})); return m;
    }
}
