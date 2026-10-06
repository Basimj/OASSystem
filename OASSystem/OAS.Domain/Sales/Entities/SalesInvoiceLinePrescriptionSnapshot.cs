using OAS.Domain.Common.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;
using OAS.Domain.Sales.ValueObjects;

namespace OAS.Domain.Sales.Entities;

public sealed class SalesInvoiceLinePrescriptionSnapshot : AuditableEntity<Guid>
{
    private SalesInvoiceLinePrescriptionSnapshot() { }

    private SalesInvoiceLinePrescriptionSnapshot(
        Guid id, Guid salesInvoiceLineId, Guid? prescriptionRevisionId, EyeSide eye,
        decimal? sph, decimal? cyl, short? axis, decimal? add, decimal? prism,
        PrismBaseDirection? prismBase, decimal? pd, decimal? monocularPd, string? va,
        decimal? fittingHeight)
    {
        Id = SalesDomainGuard.Required(id, "Prescription snapshot id");
        SalesInvoiceLineId = SalesDomainGuard.Required(salesInvoiceLineId, "Sales invoice line id");
        if (prescriptionRevisionId == Guid.Empty)
            throw new OAS.Domain.Exceptions.DomainException("Prescription revision id cannot be empty.");
        PrescriptionRevisionId = prescriptionRevisionId;
        SalesDomainGuard.Defined(eye, "Eye side");
        if (prismBase.HasValue)
            SalesDomainGuard.Defined(prismBase.Value, "Prism base direction");

        PrescriptionOpticalRules.ValidateStructural(sph, cyl, axis, add, prism, pd, monocularPd, fittingHeight);

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
        IsActive = true;
    }

    public Guid SalesInvoiceLineId { get; private set; }
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
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static SalesInvoiceLinePrescriptionSnapshot Create(
        Guid id, Guid salesInvoiceLineId, Guid? prescriptionRevisionId, EyeSide eye,
        decimal? sph = null, decimal? cyl = null, short? axis = null, decimal? add = null,
        decimal? prism = null, PrismBaseDirection? prismBase = null, decimal? pd = null,
        decimal? monocularPd = null, string? va = null, decimal? fittingHeight = null) =>
        new(id, salesInvoiceLineId, prescriptionRevisionId, eye, sph, cyl, axis, add, prism, prismBase, pd, monocularPd, va, fittingHeight);

    public void ValidateAgainst(OpticalPrescriptionRangePolicy policy) =>
        PrescriptionOpticalRules.ValidateConfiguredRanges(SPH, CYL, ADD, policy);
}
