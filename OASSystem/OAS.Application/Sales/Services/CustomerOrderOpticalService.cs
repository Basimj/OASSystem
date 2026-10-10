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
                seed.OpticalPolicy,
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

        // For prescription lenses, an exact LensVariantDetail is an optional stocked fulfillment
        // optimization, not the identity of the prescription itself. If an exact SKU exists, use it
        // and keep the inventory path. If it does not exist, preserve the commercial seed variant
        // and route the line as a non-inventory production/lab line instead of rejecting the order.
        // The prescription range has already been validated above, so the fallback is allowed only
        // for a structurally/configurationally valid prescription.
        SalesLineResolution resolved = seed;
        if (seed.ProductVariantId.HasValue)
        {
            var exact = await lensVariantResolver.TryResolveExactAsync(
                new LensVariantMatchRequest(seed.ProductVariantId.Value, draft.SPH, draft.CYL, draft.ADD),
                cancellationToken);

            if (exact is not null && exact.ProductVariantId != seed.ProductVariantId)
            {
                resolved = await lineResolver.ResolveAsync(
                    lineType,
                    exact.ProductVariantId,
                    warehouseId,
                    description,
                    cancellationToken);
            }
            else if (exact is null)
            {
                // Do not keep the seed warehouse in the fallback. A warehouse reference makes the
                // sales line inventory-bearing, which would reserve/post the seed SKU even though it
                // does not match the customer's prescription. Keeping the variant only preserves the
                // selected commercial lens/product and its price for the lab job and invoice.
                resolved = seed with
                {
                    WarehouseId = null,
                    IsStockItem = false
                };
            }
        }

        if (resolved.OpticalPolicy is not null)
            PrescriptionOpticalRules.ValidateConfiguredRanges(draft.SPH, draft.CYL, draft.ADD, resolved.OpticalPolicy);

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
                seed.OpticalPolicy,
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
                seed.OpticalPolicy,
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

        if (seed.OpticalPolicy is not null)
            PrescriptionOpticalRules.ValidateConfiguredRanges(snapshotRequest.SPH, snapshotRequest.CYL, snapshotRequest.ADD, seed.OpticalPolicy);

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
