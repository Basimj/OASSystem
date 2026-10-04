using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record CustomerOrderLineOpticalSnapshotRequest(
    OpticalMeasurementSource MeasurementSource,
    Guid? PrescriptionRevisionId,
    EyeSide Eye,
    decimal? SPH = null,
    decimal? CYL = null,
    short? Axis = null,
    decimal? ADD = null,
    decimal? Prism = null,
    PrismBaseDirection? PrismBase = null,
    decimal? PD = null,
    decimal? MonocularPD = null,
    string? VA = null,
    decimal? FittingHeight = null,
    string? LensTypeSnapshot = null,
    string? MaterialSnapshot = null,
    string? CoatingSnapshot = null,
    decimal? RefractiveIndexSnapshot = null);
