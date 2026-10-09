using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.ValueObjects;

namespace OAS.Application.Sales.Abstractions;

public sealed record SalesLineResolution(
    Guid? ProductVariantId,
    Guid? WarehouseId,
    string? ProductCode,
    string ProductName,
    string Description,
    string? UnitName,
    decimal BaseUnitPrice,
    bool PrescriptionRequired,
    OpticalPrescriptionRangePolicy? OpticalPolicy,
    bool IsStockItem = false);

public interface ISalesLineResolver
{
    Task<SalesLineResolution> ResolveAsync(
        SalesLineType lineType,
        Guid? productVariantId,
        Guid? warehouseId,
        string? requestedDescription,
        CancellationToken cancellationToken = default);
}
