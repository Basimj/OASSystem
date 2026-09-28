using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class PrescriptionRevision : AuditableEntity<Guid>
{
    private readonly List<PrescriptionEyeDetail> _eyeDetails = [];

    private PrescriptionRevision() { }

    private PrescriptionRevision(
        Guid id, Guid prescriptionId, int revisionNumber, DateOnly effectiveDate,
        string? reason, bool isCurrent, bool isActive)
    {
        Id = SalesDomainGuard.Required(id, "Prescription revision id");
        PrescriptionId = SalesDomainGuard.Required(prescriptionId, "Prescription id");
        if (revisionNumber <= 0)
            throw new DomainException("Revision number must be greater than zero.");

        RevisionNumber = revisionNumber;
        EffectiveDate = effectiveDate;
        Reason = SalesDomainGuard.Optional(reason, 500, "Revision reason");
        IsCurrent = isCurrent;
        IsActive = isActive;
    }

    public Guid PrescriptionId { get; private set; }
    public int RevisionNumber { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public string? Reason { get; private set; }
    public bool IsCurrent { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<PrescriptionEyeDetail> EyeDetails => _eyeDetails.AsReadOnly();

    public static PrescriptionRevision Create(
        Guid id, Guid prescriptionId, int revisionNumber, DateOnly effectiveDate,
        string? reason = null, bool isCurrent = true, bool isActive = true) =>
        new(id, prescriptionId, revisionNumber, effectiveDate, reason, isCurrent, isActive);

    public PrescriptionEyeDetail SetEyeDetail(
        Guid detailId, EyeSide eye, decimal? sph = null, decimal? cyl = null, short? axis = null,
        decimal? add = null, decimal? prism = null, PrismBaseDirection? prismBase = null,
        decimal? pd = null, decimal? monocularPd = null, string? va = null,
        decimal? fittingHeight = null, string? notes = null)
    {
        EnsureActive();
        SalesDomainGuard.Defined(eye, "Eye side");

        var existing = _eyeDetails.SingleOrDefault(x => x.Eye == eye);
        if (existing is not null)
        {
            existing.UpdateMeasurements(sph, cyl, axis, add, prism, prismBase, pd, monocularPd, va, fittingHeight, notes);
            return existing;
        }

        var detail = PrescriptionEyeDetail.Create(detailId, Id, eye, sph, cyl, axis, add, prism, prismBase, pd, monocularPd, va, fittingHeight, notes);
        _eyeDetails.Add(detail);
        return detail;
    }

    public void MarkCurrent()
    {
        EnsureActive();
        IsCurrent = true;
    }

    public void MarkSuperseded() => IsCurrent = false;

    public void Deactivate()
    {
        IsCurrent = false;
        IsActive = false;
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("Inactive prescription revisions cannot be modified.");
    }
}
