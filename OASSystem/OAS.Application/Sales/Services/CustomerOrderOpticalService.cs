using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Application.Sales.Services;

public sealed class CustomerOrderOpticalService(
    ISalesLineResolver lineResolver,
    ISalesPrescriptionValidator prescriptionValidator,
    ILensVariantResolver lensVariantResolver) : ICustomerOrderOpticalService
{
    public async Task<PreparedCustomerOrderLine> PrepareAsync(
        SalesLineType lineType,
        Guid? productVariantId,
        Guid? warehouseId,
        string? description,
        Guid? requestPrescriptionRevisionId,
        EyeSide? requestPrescriptionEye,
        Guid? orderPrescriptionRevisionId,
        CustomerOrderLineOpticalSnapshotRequest? snapshotRequest,
        CancellationToken cancellationToken = default)
    {
        var seed = await lineResolver.ResolveAsync(
            lineType,
            productVariantId,
            warehouseId,
            description,
            cancellationToken);

        if (lineType != SalesLineType.Lens)
        {
            if (snapshotRequest is not null)
                throw new ConflictException("sales_optical_snapshot_not_allowed", "القياسات البصرية مسموحة فقط لسطر العدسة.");

            var eye = requestPrescriptionEye;
            var revision = requestPrescriptionRevisionId;
            if (seed.PrescriptionRequired && !revision.HasValue)
                revision = orderPrescriptionRevisionId;

            await prescriptionValidator.ValidateLineAsync(
                revision,
                eye,
                seed.PrescriptionRequired,
                cancellationToken);

            return new PreparedCustomerOrderLine(seed, seed, revision, eye, null);
        }

        var draft = await ResolveSnapshotAsync(
            seed,
            requestPrescriptionRevisionId,
            requestPrescriptionEye,
            orderPrescriptionRevisionId,
            snapshotRequest,
            cancellationToken);

        if (draft is null)
            return new PreparedCustomerOrderLine(seed, seed, null, null, null);

        // Prescription lens fulfillment is based on an exact LensDetails optical identity.
        // The exact ProductVariant always remains inventory-bearing. Availability then decides
        // whether it is reserved immediately for the lab or converted into CustomerDemand
        // for purchasing. Missing catalog identity is a master-data error, not a lab fallback.
        SalesLineResolution resolved = seed;
        if (seed.ProductVariantId.HasValue)
        {
            var exact = await lensVariantResolver.ResolveAsync(
                new LensVariantMatchRequest(seed.ProductVariantId.Value, draft.SPH, draft.CYL, draft.ADD),
                cancellationToken);

            if (exact.ProductVariantId != seed.ProductVariantId)
            {
                resolved = await lineResolver.ResolveAsync(
                    lineType,
                    exact.ProductVariantId,
                    warehouseId,
                    description,
                    cancellationToken);
            }
        }

        return new PreparedCustomerOrderLine(
            seed,
            resolved,
            draft.MeasurementSource == OpticalMeasurementSource.StoredPrescription ? draft.PrescriptionRevisionId : null,
            draft.MeasurementSource == OpticalMeasurementSource.StoredPrescription ? draft.Eye : null,
            draft);
    }

    private async Task<OpticalSnapshotDraft?> ResolveSnapshotAsync(
        SalesLineResolution seed,
        Guid? requestPrescriptionRevisionId,
        EyeSide? requestPrescriptionEye,
        Guid? orderPrescriptionRevisionId,
        CustomerOrderLineOpticalSnapshotRequest? snapshotRequest,
        CancellationToken cancellationToken)
    {
        if (snapshotRequest is null && !seed.PrescriptionRequired)
            return null;

        if (snapshotRequest is null)
        {
            var revisionId = requestPrescriptionRevisionId ?? orderPrescriptionRevisionId;
            var detail = await prescriptionValidator.ValidateLineAsync(
                revisionId,
                requestPrescriptionEye,
                true,
                cancellationToken)
                ?? throw new ConflictException("sales_prescription_required", "يجب تحديد قياسات الوصفة للعدسة الطبية.");

            return FromStoredPrescription(revisionId!.Value, detail);
        }

        var source = (OpticalMeasurementSource)(byte)snapshotRequest.MeasurementSource;
        if (!Enum.IsDefined(source))
            throw new ConflictException("sales_optical_measurement_source_invalid", "مصدر القياسات البصرية غير صالح.");

        if (source == OpticalMeasurementSource.StoredPrescription)
        {
            var revisionId = snapshotRequest.PrescriptionRevisionId
                             ?? requestPrescriptionRevisionId
                             ?? orderPrescriptionRevisionId;
            var eye = (EyeSide)(byte)snapshotRequest.Eye;
            var detail = await prescriptionValidator.ValidateLineAsync(
                revisionId,
                eye,
                true,
                cancellationToken)
                ?? throw new ConflictException("sales_prescription_required", "تعذر تحميل قياسات الوصفة المخزنة.");

            var stored = FromStoredPrescription(revisionId!.Value, detail);
            return stored with
            {
                LensTypeSnapshot = snapshotRequest.LensTypeSnapshot,
                MaterialSnapshot = snapshotRequest.MaterialSnapshot,
                CoatingSnapshot = snapshotRequest.CoatingSnapshot,
                RefractiveIndexSnapshot = NormalizeLegacyRefractiveIndex(snapshotRequest.RefractiveIndexSnapshot)
            };
        }

        var prismBase = snapshotRequest.PrismBase.HasValue
            ? (PrismBaseDirection?)(byte)snapshotRequest.PrismBase.Value
            : null;
        var manualEye = (EyeSide)(byte)snapshotRequest.Eye;

        PrescriptionOpticalRules.ValidateStructural(
            snapshotRequest.SPH,
            snapshotRequest.CYL,
            snapshotRequest.Axis,
            snapshotRequest.ADD,
            snapshotRequest.Prism,
            snapshotRequest.PD,
            snapshotRequest.MonocularPD,
            snapshotRequest.FittingHeight);

        return new OpticalSnapshotDraft(
            OpticalMeasurementSource.Manual,
            null,
            manualEye,
            snapshotRequest.SPH,
            snapshotRequest.CYL,
            snapshotRequest.Axis,
            snapshotRequest.ADD,
            snapshotRequest.Prism,
            prismBase,
            snapshotRequest.PD,
            snapshotRequest.MonocularPD,
            snapshotRequest.VA,
            snapshotRequest.FittingHeight,
            snapshotRequest.LensTypeSnapshot,
            snapshotRequest.MaterialSnapshot,
            snapshotRequest.CoatingSnapshot,
            NormalizeLegacyRefractiveIndex(snapshotRequest.RefractiveIndexSnapshot));
    }

    private static decimal? NormalizeLegacyRefractiveIndex(decimal? value)
        => value.HasValue && value.Value <= 0m ? null : value;

    private static OpticalSnapshotDraft FromStoredPrescription(
        Guid revisionId,
        OAS.Domain.Sales.Entities.PrescriptionEyeDetail detail) =>
        new(
            OpticalMeasurementSource.StoredPrescription,
            revisionId,
            detail.Eye,
            detail.SPH,
            detail.CYL,
            detail.Axis,
            detail.ADD,
            detail.Prism,
            detail.PrismBase,
            detail.PD,
            detail.MonocularPD,
            detail.VA,
            detail.FittingHeight,
            null,
            null,
            null,
            null);
}
