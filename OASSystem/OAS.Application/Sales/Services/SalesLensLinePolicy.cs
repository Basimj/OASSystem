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
            // Every sold lens goes through the optical lab/production workflow. Stock availability
            // only decides whether the exact SKU can be reserved now or must first be procured via
            // CustomerDemand; it never bypasses laboratory preparation.
            return true;
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
