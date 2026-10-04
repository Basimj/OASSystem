using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

internal static class CustomerOrderOpticalSnapshotFactory
{
    public static CustomerOrderLineOpticalSnapshot Create(Guid lineId, OpticalSnapshotDraft draft)
    {
        return draft.MeasurementSource == OpticalMeasurementSource.StoredPrescription
            ? CustomerOrderLineOpticalSnapshot.CreateStoredPrescription(
                Guid.NewGuid(), lineId, draft.PrescriptionRevisionId!.Value, draft.Eye,
                draft.SPH, draft.CYL, draft.Axis, draft.ADD, draft.Prism, draft.PrismBase,
                draft.PD, draft.MonocularPD, draft.VA, draft.FittingHeight,
                draft.LensTypeSnapshot, draft.MaterialSnapshot, draft.CoatingSnapshot, draft.RefractiveIndexSnapshot)
            : CustomerOrderLineOpticalSnapshot.CreateManual(
                Guid.NewGuid(), lineId, draft.Eye,
                draft.SPH, draft.CYL, draft.Axis, draft.ADD, draft.Prism, draft.PrismBase,
                draft.PD, draft.MonocularPD, draft.VA, draft.FittingHeight,
                draft.LensTypeSnapshot, draft.MaterialSnapshot, draft.CoatingSnapshot, draft.RefractiveIndexSnapshot);
    }
}
