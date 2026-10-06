using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Purchasing.CustomerDemand;

public sealed record CustomerDemandTrackingDetailsDto(
    CustomerDemandTrackingItemDto Item,
    DateOnly? CustomerOrderRequiredDate,
    string? VariantDescription,
    string? Notes,
    OpticalMeasurementSource? MeasurementSource,
    EyeSide? Eye,
    decimal? SPH,
    decimal? CYL,
    short? Axis,
    decimal? ADD,
    decimal? Prism,
    PrismBaseDirection? PrismBase,
    decimal? PD,
    decimal? MonocularPD,
    decimal? FittingHeight,
    string? LensType,
    string? Material,
    string? Coating,
    PurchaseOrderStatus? PurchaseOrderStatus,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal RemainingQuantity,
    DateOnly? LastReceiptDate,
    decimal? UnitPrice,
    decimal? ActualUnitCost,
    string RowVersion = "");
