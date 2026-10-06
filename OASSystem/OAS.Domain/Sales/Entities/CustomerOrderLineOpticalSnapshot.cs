using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;
using OAS.Domain.Sales.ValueObjects;

namespace OAS.Domain.Sales.Entities;

public sealed class CustomerOrderLineOpticalSnapshot : AuditableEntity<Guid>
{
    private CustomerOrderLineOpticalSnapshot() { }

    private CustomerOrderLineOpticalSnapshot(
        Guid id,
        Guid customerOrderLineId,
        OpticalMeasurementSource measurementSource,
        Guid? prescriptionRevisionId,
        EyeSide eye,
        decimal? sph,
        decimal? cyl,
        short? axis,
        decimal? add,
        decimal? prism,
        PrismBaseDirection? prismBase,
        decimal? pd,
        decimal? monocularPd,
        string? va,
        decimal? fittingHeight,
        string? lensTypeSnapshot,
        string? materialSnapshot,
        string? coatingSnapshot,
        decimal? refractiveIndexSnapshot)
    {
        Id = SalesDomainGuard.Required(id, "Optical snapshot id");
        CustomerOrderLineId = SalesDomainGuard.Required(customerOrderLineId, "Customer order line id");
        SetMeasurements(
            measurementSource,
            prescriptionRevisionId,
            eye,
            sph,
            cyl,
            axis,
            add,
            prism,
            prismBase,
            pd,
            monocularPd,
            va,
            fittingHeight,
            lensTypeSnapshot,
            materialSnapshot,
            coatingSnapshot,
            refractiveIndexSnapshot);
        IsActive = true;
    }

    public Guid CustomerOrderLineId { get; private set; }
    public OpticalMeasurementSource MeasurementSource { get; private set; }
    public Guid? PrescriptionRevisionId { get; private set; }
    public EyeSide Eye { get; private set; }
    public decimal? SPH { get; private set; }
    public decimal? CYL { get; private set; }
    public short? Axis { get; private set; }
    public decimal? ADD { get; private set; }
    public decimal? Prism { get; private set; }
    public PrismBaseDirection? PrismBase { get; private set; }
    public decimal? PD { get; private set; }
    public decimal? MonocularPD { get; private set; }
    public string? VA { get; private set; }
    public decimal? FittingHeight { get; private set; }
    public string? LensTypeSnapshot { get; private set; }
    public string? MaterialSnapshot { get; private set; }
    public string? CoatingSnapshot { get; private set; }
    public decimal? RefractiveIndexSnapshot { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static CustomerOrderLineOpticalSnapshot CreateStoredPrescription(
        Guid id,
        Guid customerOrderLineId,
        Guid prescriptionRevisionId,
        EyeSide eye,
        decimal? sph = null,
        decimal? cyl = null,
        short? axis = null,
        decimal? add = null,
        decimal? prism = null,
        PrismBaseDirection? prismBase = null,
        decimal? pd = null,
        decimal? monocularPd = null,
        string? va = null,
        decimal? fittingHeight = null,
        string? lensTypeSnapshot = null,
        string? materialSnapshot = null,
        string? coatingSnapshot = null,
        decimal? refractiveIndexSnapshot = null) =>
        new(
            id,
            customerOrderLineId,
            OpticalMeasurementSource.StoredPrescription,
            prescriptionRevisionId,
            eye,
            sph,
            cyl,
            axis,
            add,
            prism,
            prismBase,
            pd,
            monocularPd,
            va,
            fittingHeight,
            lensTypeSnapshot,
            materialSnapshot,
            coatingSnapshot,
            refractiveIndexSnapshot);

    public static CustomerOrderLineOpticalSnapshot CreateManual(
        Guid id,
        Guid customerOrderLineId,
        EyeSide eye,
        decimal? sph = null,
        decimal? cyl = null,
        short? axis = null,
        decimal? add = null,
        decimal? prism = null,
        PrismBaseDirection? prismBase = null,
        decimal? pd = null,
        decimal? monocularPd = null,
        string? va = null,
        decimal? fittingHeight = null,
        string? lensTypeSnapshot = null,
        string? materialSnapshot = null,
        string? coatingSnapshot = null,
        decimal? refractiveIndexSnapshot = null) =>
        new(
            id,
            customerOrderLineId,
            OpticalMeasurementSource.Manual,
            null,
            eye,
            sph,
            cyl,
            axis,
            add,
            prism,
            prismBase,
            pd,
            monocularPd,
            va,
            fittingHeight,
            lensTypeSnapshot,
            materialSnapshot,
            coatingSnapshot,
            refractiveIndexSnapshot);

    public void UpdateWhileDraft(
        CustomerOrderStatus orderStatus,
        OpticalMeasurementSource measurementSource,
        Guid? prescriptionRevisionId,
        EyeSide eye,
        decimal? sph,
        decimal? cyl,
        short? axis,
        decimal? add,
        decimal? prism,
        PrismBaseDirection? prismBase,
        decimal? pd,
        decimal? monocularPd,
        string? va,
        decimal? fittingHeight,
        string? lensTypeSnapshot,
        string? materialSnapshot,
        string? coatingSnapshot,
        decimal? refractiveIndexSnapshot)
    {
        if (orderStatus != CustomerOrderStatus.Draft)
            throw new DomainException("Optical snapshot can only be changed while the customer order is draft.");

        SetMeasurements(
            measurementSource,
            prescriptionRevisionId,
            eye,
            sph,
            cyl,
            axis,
            add,
            prism,
            prismBase,
            pd,
            monocularPd,
            va,
            fittingHeight,
            lensTypeSnapshot,
            materialSnapshot,
            coatingSnapshot,
            refractiveIndexSnapshot);
    }

    public void DeactivateWhileDraft(CustomerOrderStatus orderStatus)
    {
        if (orderStatus != CustomerOrderStatus.Draft)
            throw new DomainException("Optical snapshot can only be deactivated while the customer order is draft.");
        IsActive = false;
    }

    public void ValidateAgainst(OpticalPrescriptionRangePolicy policy) =>
        PrescriptionOpticalRules.ValidateConfiguredRanges(SPH, CYL, ADD, policy);

    private void SetMeasurements(
        OpticalMeasurementSource measurementSource,
        Guid? prescriptionRevisionId,
        EyeSide eye,
        decimal? sph,
        decimal? cyl,
        short? axis,
        decimal? add,
        decimal? prism,
        PrismBaseDirection? prismBase,
        decimal? pd,
        decimal? monocularPd,
        string? va,
        decimal? fittingHeight,
        string? lensTypeSnapshot,
        string? materialSnapshot,
        string? coatingSnapshot,
        decimal? refractiveIndexSnapshot)
    {
        SalesDomainGuard.Defined(measurementSource, "Optical measurement source");
        SalesDomainGuard.Defined(eye, "Eye side");
        if (prismBase.HasValue)
            SalesDomainGuard.Defined(prismBase.Value, "Prism base direction");
        if (measurementSource == OpticalMeasurementSource.StoredPrescription &&
            (!prescriptionRevisionId.HasValue || prescriptionRevisionId.Value == Guid.Empty))
            throw new DomainException("Stored prescription measurements require a prescription revision.");
        if (measurementSource == OpticalMeasurementSource.Manual && prescriptionRevisionId.HasValue)
            throw new DomainException("Manual optical measurements cannot reference a prescription revision.");
        if (prescriptionRevisionId == Guid.Empty)
            throw new DomainException("Prescription revision id cannot be empty.");
        if (refractiveIndexSnapshot.HasValue && refractiveIndexSnapshot.Value <= 0)
            throw new DomainException("Refractive index must be greater than zero when provided.");

        PrescriptionOpticalRules.ValidateStructural(sph, cyl, axis, add, prism, pd, monocularPd, fittingHeight);

        MeasurementSource = measurementSource;
        PrescriptionRevisionId = prescriptionRevisionId;
        Eye = eye;
        SPH = sph;
        CYL = cyl;
        Axis = axis;
        ADD = add;
        Prism = prism;
        PrismBase = prism is null or 0m ? null : prismBase;
        PD = pd;
        MonocularPD = monocularPd;
        VA = SalesDomainGuard.Optional(va, 20, "Visual acuity");
        FittingHeight = fittingHeight;
        LensTypeSnapshot = SalesDomainGuard.Optional(lensTypeSnapshot, 100, "Lens type snapshot");
        MaterialSnapshot = SalesDomainGuard.Optional(materialSnapshot, 100, "Material snapshot");
        CoatingSnapshot = SalesDomainGuard.Optional(coatingSnapshot, 100, "Coating snapshot");
        RefractiveIndexSnapshot = refractiveIndexSnapshot;
    }
}
