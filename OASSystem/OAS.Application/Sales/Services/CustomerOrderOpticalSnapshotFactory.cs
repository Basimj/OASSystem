using OAS.Application.Sales.Abstractions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

internal static class CustomerOrderOpticalSnapshotFactory
{
    public static CustomerOrderLineOpticalSnapshot Create(Guid lineId, OpticalSnapshotDraft draft)
    {
        // Defensive normalization for legacy/invalid lens master data. The Domain invariant remains
        // strict (null or > 0), but order creation must not crash because older product records
        // stored 0 or a negative placeholder for an unknown refractive index.
        var refractiveIndexSnapshot = NormalizeRefractiveIndex(draft.RefractiveIndexSnapshot);

        return draft.MeasurementSource == OpticalMeasurementSource.StoredPrescription
            ? CustomerOrderLineOpticalSnapshot.CreateStoredPrescription(
                Guid.NewGuid(), lineId, draft.PrescriptionRevisionId!.Value, draft.Eye,
                draft.SPH, draft.CYL, draft.Axis, draft.ADD, draft.Prism, draft.PrismBase,
                draft.PD, draft.MonocularPD, draft.VA, draft.FittingHeight,
                draft.LensTypeSnapshot, draft.MaterialSnapshot, draft.CoatingSnapshot, refractiveIndexSnapshot)
            : CustomerOrderLineOpticalSnapshot.CreateManual(
                Guid.NewGuid(), lineId, draft.Eye,
                draft.SPH, draft.CYL, draft.Axis, draft.ADD, draft.Prism, draft.PrismBase,
                draft.PD, draft.MonocularPD, draft.VA, draft.FittingHeight,
                draft.LensTypeSnapshot, draft.MaterialSnapshot, draft.CoatingSnapshot, refractiveIndexSnapshot);
    }

    private static decimal? NormalizeRefractiveIndex(decimal? value)
        => value.HasValue && value.Value <= 0m ? null : value;
}
