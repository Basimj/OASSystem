using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.SalesInvoices;

namespace OAS.Application.Sales.Services;

/// <summary>
/// A direct invoice must never bypass reservation, optical production or delivery tracking.
/// Order checkout remains the only path for lines requiring production.
/// </summary>
public static class SalesInvoiceOperationalRoutingPolicy
{
    public static bool RequiresOrder(SalesInvoiceLineRequest line) =>
        line.LineType == SalesLineType.Lens ||
        line.RequiresProduction ||
        (line.LineType == SalesLineType.Frame && line.GroupId.HasValue);

    public static bool RequiresOrder(IEnumerable<SalesInvoiceLineRequest> lines) =>
        lines.Any(RequiresOrder);
}
