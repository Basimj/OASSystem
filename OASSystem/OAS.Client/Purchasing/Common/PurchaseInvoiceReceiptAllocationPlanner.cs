namespace OAS.Client.Purchasing.Common;

/// <summary>A UI-side plan for a single posted receipt. No quantity is rounded or invented.</summary>
public sealed record InvoiceReceiptDemand(Guid InvoiceLineId, Guid? PurchaseOrderLineId,
    Guid? ProductVariantId, decimal Quantity, int LineSequence, Guid? SourceReceiptLineId = null);
public sealed record ReceiptAvailableLine(Guid ReceiptLineId, Guid PurchaseOrderLineId,
    Guid ProductVariantId, decimal AcceptedQuantity, decimal ReturnedQuantity);
public sealed record PlannedReceiptAllocation(Guid InvoiceLineId, Guid ReceiptLineId, decimal Quantity);
/// <summary>One actionable invoice quantity error (line numbers remain stable across UI remapping).</summary>
public sealed record InvoiceReceiptAllocationIssue(int LineSequence, decimal RequestedQuantity,
    decimal AvailableQuantity, string Message);


public static class PurchaseInvoiceReceiptAllocationPlanner
{
    public static bool TryPlan(
        IReadOnlyList<InvoiceReceiptDemand> invoiceLines,
        IReadOnlyList<ReceiptAvailableLine> receiptLines,
        out IReadOnlyList<PlannedReceiptAllocation> allocations,
        out string error)
    {
        var success=TryPlanDetailed(invoiceLines,receiptLines,out allocations,out var issue,out error);
        return success;
    }

    public static bool TryPlanDetailed(
        IReadOnlyList<InvoiceReceiptDemand> invoiceLines,
        IReadOnlyList<ReceiptAvailableLine> receiptLines,
        out IReadOnlyList<PlannedReceiptAllocation> allocations,
        out InvoiceReceiptAllocationIssue? issue,
        out string error)
    {
        allocations = [];
        issue = null;
        error = string.Empty;
        if (invoiceLines.Count == 0)
        {
            error="أضف صنفًا واحدًا على الأقل قبل حفظ الفاتورة.";
            return false;
        }
        var remainingByReceiptLine=receiptLines.ToDictionary(x=>x.ReceiptLineId,
            x=>Math.Max(0m,x.AcceptedQuantity-x.ReturnedQuantity));
        var result=new List<PlannedReceiptAllocation>();
        foreach (var line in invoiceLines)
        {
            if (!line.PurchaseOrderLineId.HasValue || !line.ProductVariantId.HasValue || line.Quantity<=0m)
            {
                error=$"راجع الصنف والكمية في البند {line.LineSequence}.";
                issue=new(line.LineSequence,line.Quantity,0m,
                    "أدخل كمية صحيحة أكبر من صفر وتأكد من ربط الصنف بأمر الشراء.");
                return false;
            }
            // A receipt may contain multiple lines with the same product display name.
            // When the invoice was generated from a particular receipt line, preserve that
            // exact association instead of reallocating to another indistinguishable line.
            var matchingReceiptLines=receiptLines.Where(x=>
                x.PurchaseOrderLineId==line.PurchaseOrderLineId.Value &&
                x.ProductVariantId==line.ProductVariantId.Value &&
                (!line.SourceReceiptLineId.HasValue || x.ReceiptLineId==line.SourceReceiptLineId.Value)).ToArray();
            if (matchingReceiptLines.Length==0)
            {
                var details=line.SourceReceiptLineId.HasValue
                    ? "لم يُعثر على بند الاستلام الأصلي المرتبط بهذا الصنف وسطر أمر الشراء. حدّث بيانات الاستلام ثم حاول مجددًا."
                    : "لا يوجد بند استلام مطابق لهذا الصنف وسطر أمر الشراء. تأكد من اختيار الاستلام الصحيح؛ قد تتشابه أسماء الأصناف لكن تختلف بنودها.";
                error=$"البند {line.LineSequence}: {details}";
                issue=new(line.LineSequence,line.Quantity,0m,details);
                return false;
            }
            var remaining=line.Quantity;
            foreach (var receipt in matchingReceiptLines)
            {
                if (remaining<=0m) break;
                var available=remainingByReceiptLine[receipt.ReceiptLineId];
                if (available<=0m) continue;
                var take=Math.Min(available,remaining);
                result.Add(new(line.InvoiceLineId,receipt.ReceiptLineId,take));
                remainingByReceiptLine[receipt.ReceiptLineId]-=take;
                remaining-=take;
            }
            if (remaining>0m)
            {
                var available=line.Quantity-remaining;
                if(available<=0m)
                {
                    error=$"لا توجد كمية غير مفوترة للبند {line.LineSequence} في الاستلام المحدد.";
                    issue=new(line.LineSequence,line.Quantity,0m,
                        "المتاح لهذا البند صفر؛ قد تكون كميته قد فُوترت أو أُرجعت بالكامل. حدّث كميات الاستلام أو اختر استلامًا آخر. كمية البند الآخر لا تعوض هذا البند.");
                }
                else
                {
                    error=$"الكمية في البند {line.LineSequence} تتجاوز المتاح في الاستلام المحدد.";
                    issue=new(line.LineSequence,line.Quantity,available,
                        $"الكمية المطلوبة {line.Quantity:#,##0.###}، والمتاحة لهذا البند {available:#,##0.###} فقط. خفّض الكمية أو حدّث بيانات الاستلام.");
                }
                return false;
            }
        }
        allocations=result;
        return true;
    }
}
