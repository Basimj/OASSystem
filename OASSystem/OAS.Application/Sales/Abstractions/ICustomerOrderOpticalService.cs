using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.ValueObjects;

namespace OAS.Application.Sales.Abstractions;

public sealed record OpticalSnapshotDraft(
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
    decimal? RefractiveIndexSnapshot);

public sealed record PreparedCustomerOrderLine(
    SalesLineResolution Resolution,
    Guid? PrescriptionRevisionId,
    EyeSide? PrescriptionEye,
    OpticalSnapshotDraft? OpticalSnapshot);

public interface ICustomerOrderOpticalService
{
    Task<PreparedCustomerOrderLine> PrepareAsync(
        SalesLineType lineType,
        Guid? productVariantId,
        Guid? warehouseId,
        string? description,
        Guid? requestPrescriptionRevisionId,
        EyeSide? requestPrescriptionEye,
        Guid? orderPrescriptionRevisionId,
        CustomerOrderLineOpticalSnapshotRequest? snapshotRequest,
        CancellationToken cancellationToken = default);
}
