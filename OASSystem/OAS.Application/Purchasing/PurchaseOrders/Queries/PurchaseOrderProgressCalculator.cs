using OAS.Contracts.Purchasing.PurchaseOrders;

namespace OAS.Application.Purchasing.PurchaseOrders.Queries;

/// <summary>
/// Pure quantity calculation. Receipts, invoice allocations and returns must be
/// fetched from *posted* documents by the read-only SQL query service.
/// </summary>
public static class PurchaseOrderProgressCalculator
{
    public static PurchaseOrderLineProgressDto Calculate(
        Guid lineId, int sequence, string name, decimal ordered,
        decimal received, decimal invoiced, decimal returned)
    {
        var remainingToReceive = Math.Max(0m, ordered - received);
        // Returned goods must not be counted as still available for billing or return.
        var available = Math.Max(0m, received - returned);
        return new PurchaseOrderLineProgressDto(lineId, sequence, name,
            ordered, received, invoiced, returned, remainingToReceive,
            Math.Max(0m, available - invoiced), available);
    }
}
