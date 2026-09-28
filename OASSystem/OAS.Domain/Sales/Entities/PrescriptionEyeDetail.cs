using OAS.Domain.Common.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;
using OAS.Domain.Sales.ValueObjects;

namespace OAS.Domain.Sales.Entities;

public sealed class PrescriptionEyeDetail : AuditableEntity<Guid>
{
    private PrescriptionEyeDetail() { }

    private PrescriptionEyeDetail(
        Guid id,
        Guid prescriptionRevisionId,
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
        string? notes)
    {
        Id = SalesDomainGuard.Required(id, "Prescription eye detail id");
        PrescriptionRevisionId = SalesDomainGuard.Required(prescriptionRevisionId, "Prescription revision id");
        Eye = eye;
        IsActive = true;
        UpdateMeasurements(sph, cyl, axis, add, prism, prismBase, pd, monocularPd, va, fittingHeight, notes);
    }

    public Guid PrescriptionRevisionId { get; private set; }
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
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static PrescriptionEyeDetail Create(
        Guid id, Guid prescriptionRevisionId, EyeSide eye,
        decimal? sph = null, decimal? cyl = null, short? axis = null, decimal? add = null,
        decimal? prism = null, PrismBaseDirection? prismBase = null, decimal? pd = null,
        decimal? monocularPd = null, string? va = null, decimal? fittingHeight = null, string? notes = null) =>
        new(id, prescriptionRevisionId, eye, sph, cyl, axis, add, prism, prismBase, pd, monocularPd, va, fittingHeight, notes);

    public void UpdateMeasurements(
        decimal? sph, decimal? cyl, short? axis, decimal? add, decimal? prism,
        PrismBaseDirection? prismBase, decimal? pd, decimal? monocularPd, string? va,
        decimal? fittingHeight, string? notes)
    {
        SalesDomainGuard.Defined(Eye, "Eye side");
        if (prismBase.HasValue)
            SalesDomainGuard.Defined(prismBase.Value, "Prism base direction");

        PrescriptionOpticalRules.ValidateStructural(sph, cyl, axis, add, prism, pd, monocularPd, fittingHeight);

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
        Notes = SalesDomainGuard.Optional(notes, 500, "Eye notes");
    }

    public void ValidateAgainst(OpticalPrescriptionRangePolicy policy) =>
        PrescriptionOpticalRules.ValidateConfiguredRanges(SPH, CYL, ADD, policy);

}
