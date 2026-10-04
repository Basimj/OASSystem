using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record CustomerOrderLineOpticalSnapshotDto(
    Guid Id,
    Guid CustomerOrderLineId,
    OpticalMeasurementSource MeasurementSource,
    Guid? PrescriptionRevisionId,
    EyeSide Eye,
    decimal? SPH,
    decimal? CYL,
    short? Axis,
    decimal? ADD,
    decimal? Prism,
    PrismBaseDirection? PrismBase,
    decimal? PD,
    decimal? MonocularPD,
    string? VA,
    decimal? FittingHeight,
    string? LensTypeSnapshot,
    string? MaterialSnapshot,
    string? CoatingSnapshot,
    decimal? RefractiveIndexSnapshot,
    bool IsActive,
    string RowVersion,
    DateTimeOffset CreatedAtUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedAtUtc,
    string? LastModifiedBy);
