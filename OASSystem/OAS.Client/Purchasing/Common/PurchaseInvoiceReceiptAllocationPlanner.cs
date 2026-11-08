namespace OAS.Client.Purchasing.Common;

/// <summary>A UI-side plan for a single posted receipt. No quantity is rounded or invented.</summary>
public sealed record InvoiceReceiptDemand(Guid InvoiceLineId, Guid? PurchaseOrderLineId,
    Guid? ProductVariantId, decimal Quantity, int LineSequence);
public sealed record ReceiptAvailableLine(Guid ReceiptLineId, Guid PurchaseOrderLineId,
    Guid ProductVariantId, decimal AcceptedQuantity, decimal ReturnedQuantity);
public sealed record PlannedReceiptAllocation(Guid InvoiceLineId, Guid ReceiptLineId, decimal Quantity);

public static class PurchaseInvoiceReceiptAllocationPlanner
{
    public static bool TryPlan(
        IReadOnlyList<InvoiceReceiptDemand> invoiceLines,
        IReadOnlyList<ReceiptAvailableLine> receiptLines,
        out IReadOnlyList<PlannedReceiptAllocation> allocations,
        out string error)
    {
        allocations = [];
        error = string.Empty;
        if(invoiceLines.Count==0)
        {
            error="أضف صنفًا واحدًا على الأقل قبل حفظ الفاتورة.";
            return false;
        }
        var remainingByReceiptLine=receiptLines.ToDictionary(x=>x.ReceiptLineId,
            x=>Math.Max(0m,x.AcceptedQuantity-x.ReturnedQuantity));
        var result=new List<PlannedReceiptAllocation>();
        foreach(var line in invoiceLines)
        {
            if(!line.PurchaseOrderLineId.HasValue || !line.ProductVariantId.HasValue || line.Quantity<=0m)
            {
                error=$"راجع الصنف والكمية في البند {line.LineSequence}.";
                return false;
            }
            var remaining=line.Quantity;
            foreach(var receipt in receiptLines.Where(x=>x.PurchaseOrderLineId==line.PurchaseOrderLineId.Value &&
                         x.ProductVariantId==line.ProductVariantId.Value))
            {
                if(remaining<=0m)break;
                var available=remainingByReceiptLine[receipt.ReceiptLineId];
                if(available<=0m)continue;
                var take=Math.Min(available,remaining);
                result.Add(new(line.InvoiceLineId,receipt.ReceiptLineId,take));
                remainingByReceiptLine[receipt.ReceiptLineId]-=take;
                remaining-=take;
            }
            if(remaining>0m)
            {
                error=$"الكمية في البند {line.LineSequence} أكبر من الكمية المقبولة المتاحة في الاستلام المحدد. خفّض الكمية أو اختر استلامًا آخر.";
                return false;
            }
        }
        allocations=result;
        return true;
    }
}
