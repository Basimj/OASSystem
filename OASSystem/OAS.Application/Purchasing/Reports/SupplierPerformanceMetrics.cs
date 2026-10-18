namespace OAS.Application.Purchasing.Reports;

public static class SupplierPerformanceMetrics
{
    public static decimal OpenBaseQuantity(decimal orderedBaseQuantity, decimal postedAcceptedBaseQuantity, bool isClosed)
        => isClosed ? 0m : Math.Max(0m, Math.Round(orderedBaseQuantity - postedAcceptedBaseQuantity, 3));

    public static decimal NetReceivedBaseQuantity(decimal acceptedBaseQuantity, decimal returnedBaseQuantity)
        => Math.Max(0m, Math.Round(acceptedBaseQuantity - returnedBaseQuantity, 3));

    public static decimal GrniBaseAmount(decimal receiptCostBase, decimal matchedReceiptCostBase, decimal unmatchedReturnCostBase)
        => Math.Max(0m, Math.Round(receiptCostBase - matchedReceiptCostBase - unmatchedReturnCostBase, 4));

    public static decimal PurchasePriceVarianceBase(decimal postedInvoiceNetBase, decimal matchedReceiptCostBase)
        => Math.Round(postedInvoiceNetBase - matchedReceiptCostBase, 4, MidpointRounding.AwayFromZero);

    public static bool IsLate(decimal openBaseQuantity, DateOnly? expectedDeliveryDate, DateOnly asOfDate)
        => openBaseQuantity > 0m && expectedDeliveryDate.HasValue && expectedDeliveryDate.Value < asOfDate;

    public static bool WasDeliveredLate(DateOnly? expectedDeliveryDate, IEnumerable<DateOnly> actualReceiptDates)
        => expectedDeliveryDate.HasValue && actualReceiptDates.Any(date => date > expectedDeliveryDate.Value);

    public static decimal? WeightedAverageDays(IEnumerable<(decimal Weight, decimal Days)> values)
    {
        var valid = values.Where(x => x.Weight > 0m).ToArray();
        var totalWeight = valid.Sum(x => x.Weight);
        if (totalWeight <= 0m) return null;
        return Math.Round(valid.Sum(x => x.Weight * x.Days) / totalWeight, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal? WeightedPercent(IEnumerable<(decimal Weight, bool Success)> values)
    {
        var valid = values.Where(x => x.Weight > 0m).ToArray();
        var totalWeight = valid.Sum(x => x.Weight);
        if (totalWeight <= 0m) return null;
        var successWeight = valid.Where(x => x.Success).Sum(x => x.Weight);
        return Math.Round(successWeight * 100m / totalWeight, 2, MidpointRounding.AwayFromZero);
    }
}
