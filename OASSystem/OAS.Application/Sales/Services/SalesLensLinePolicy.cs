using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

/// <summary>
/// Central policy for optical sales-line behavior. The server remains authoritative so a client
/// cannot bypass laboratory routing or accidentally retain the seed-SKU price after exact-lens resolution.
/// </summary>
public static class SalesLensLinePolicy
{
    public static bool ResolveRequiresProduction(
        SalesLineType lineType,
        SalesLineResolution resolution,
        Guid? groupId,
        bool requestedValue)
    {
        if (lineType == SalesLineType.Lens)
        {
            // Prescription lenses always require optical preparation. Non-stock lenses represent
            // made-to-order/lab supply and also require production. A grouped lens belongs to a
            // Frame + OD + OS optical set and therefore requires fitting even when plano/non-prescription.
            return resolution.PrescriptionRequired || !resolution.IsStockItem || groupId.HasValue;
        }

        // A frame that belongs to an optical set is carried into the job as fitting context.
        if (lineType == SalesLineType.Frame && groupId.HasValue)
            return true;

        return requestedValue;
    }

    public static decimal ResolveCustomerOrderActualPrice(
        decimal requestedActualPrice,
        decimal seedStandardPrice,
        decimal exactStandardPrice,
        byte currencyDecimalPlaces)
    {
        // If exact-lens resolution changed SKU and the user had not changed the displayed seed price,
        // move the transaction price to the exact SKU automatically. If the user intentionally changed
        // the price, preserve that explicit price.
        if (PricesEqual(requestedActualPrice, seedStandardPrice, currencyDecimalPlaces))
            return exactStandardPrice;

        return requestedActualPrice;
    }

    private static bool PricesEqual(decimal left, decimal right, byte decimals)
    {
        var roundedLeft = decimal.Round(left, decimals, MidpointRounding.AwayFromZero);
        var roundedRight = decimal.Round(right, decimals, MidpointRounding.AwayFromZero);
        return roundedLeft == roundedRight;
    }
}
