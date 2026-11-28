namespace OAS.Contracts.Inventory.Reports;

/// <summary>Aggregate only warehouse-level snapshots; unknown monetary values remain unknown.</summary>
public static class WarehouseReportAggregation
{
    public static decimal? SumKnown(IEnumerable<decimal?> values)
    {
        decimal total = 0;
        foreach (var value in values) { if (!value.HasValue) return null; total += value.Value; }
        return total;
    }

    public static decimal? ReservedValue(decimal? reserved, decimal? averageCost) =>
        reserved == 0m ? 0m : reserved * averageCost;

    public static decimal? AvailableValue(decimal? inventoryValue, decimal? reservedValue) => inventoryValue - reservedValue;

    public static bool MatchesStockState(string state, decimal onHand, decimal? reserved, bool current) => state switch
    {
        "available" => current ? onHand - reserved > 0 : onHand > 0,
        "out" => onHand <= 0,
        "reserved" => current && reserved > 0,
        "partial" => current && reserved > 0 && reserved < onHand,
        "full" => current && onHand > 0 && reserved >= onHand,
        _ => true
    };

    public static bool IsReservationState(string state) => state is "reserved" or "partial" or "full";

    public static WarehouseFlow SumFlows(IEnumerable<WarehouseFlow> source)
    {
        var rows = source.ToArray();
        return new(SumKnown(rows.Select(x=>x.Opening)), rows.Sum(x=>x.Purchases), rows.Sum(x=>x.OtherIn),
            rows.Sum(x=>x.TransferIn), rows.Sum(x=>x.SalesReturns), rows.Sum(x=>x.Sales),
            rows.Sum(x=>x.PurchaseReturns), rows.Sum(x=>x.TransferOut), rows.Sum(x=>x.OtherOut), SumKnown(rows.Select(x=>x.Closing)), rows.Sum(x=>x.OpeningIn));
    }
}
