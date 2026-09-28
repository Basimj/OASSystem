using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class Prescription : AuditableEntity<Guid>
{
    private readonly List<PrescriptionRevision> _revisions = [];

    private Prescription() { }

    private Prescription(
        Guid id, string prescriptionCode, Guid customerId, DateOnly prescriptionDate,
        string? prescribedBy, string? clinicName, string? notes)
    {
        Id = SalesDomainGuard.Required(id, "Prescription id");
        PrescriptionCode = SalesDomainGuard.Required(prescriptionCode, 40, "Prescription code");
        CustomerId = SalesDomainGuard.Required(customerId, "Customer id");
        PrescriptionDate = prescriptionDate;
        PrescribedBy = SalesDomainGuard.Optional(prescribedBy, 150, "Prescribed by");
        ClinicName = SalesDomainGuard.Optional(clinicName, 150, "Clinic name");
        Notes = SalesDomainGuard.Optional(notes, 1000, "Prescription notes");
        Status = PrescriptionStatus.Draft;
        IsActive = true;
    }

    public string PrescriptionCode { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public DateOnly PrescriptionDate { get; private set; }
    public PrescriptionStatus Status { get; private set; }
    public string? PrescribedBy { get; private set; }
    public string? ClinicName { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<PrescriptionRevision> Revisions => _revisions.AsReadOnly();

    public static Prescription Create(
        Guid id, string prescriptionCode, Guid customerId, DateOnly prescriptionDate,
        string? prescribedBy = null, string? clinicName = null, string? notes = null) =>
        new(id, prescriptionCode, customerId, prescriptionDate, prescribedBy, clinicName, notes);

    public void UpdateDetails(
        DateOnly prescriptionDate, string? prescribedBy, string? clinicName, string? notes)
    {
        EnsureEditable();
        PrescriptionDate = prescriptionDate;
        PrescribedBy = SalesDomainGuard.Optional(prescribedBy, 150, "Prescribed by");
        ClinicName = SalesDomainGuard.Optional(clinicName, 150, "Clinic name");
        Notes = SalesDomainGuard.Optional(notes, 1000, "Prescription notes");
    }

    public PrescriptionRevision AddRevision(
        Guid revisionId, DateOnly effectiveDate, string? reason = null)
    {
        EnsureEditable();

        foreach (var current in _revisions.Where(x => x.IsCurrent))
            current.MarkSuperseded();

        var nextRevision = _revisions.Count == 0 ? 1 : _revisions.Max(x => x.RevisionNumber) + 1;
        var revision = PrescriptionRevision.Create(revisionId, Id, nextRevision, effectiveDate, reason, true, true);
        _revisions.Add(revision);
        return revision;
    }

    public void SetStatus(PrescriptionStatus status)
    {
        SalesDomainGuard.Defined(status, "Prescription status");
        if (status == Status)
            return;

        var allowed = Status switch
        {
            PrescriptionStatus.Draft => status is PrescriptionStatus.Active or PrescriptionStatus.Cancelled,
            PrescriptionStatus.Active => status is PrescriptionStatus.Superseded or PrescriptionStatus.Cancelled,
            _ => false
        };

        if (!allowed)
            throw new DomainException($"Invalid prescription status transition from {Status} to {status}.");

        Status = status;
        IsActive = status is PrescriptionStatus.Draft or PrescriptionStatus.Active;

        if (!IsActive)
        {
            foreach (var revision in _revisions.Where(x => x.IsCurrent))
                revision.MarkSuperseded();
        }
    }

    private void EnsureEditable()
    {
        if (Status is PrescriptionStatus.Superseded or PrescriptionStatus.Cancelled)
            throw new DomainException("Superseded or cancelled prescriptions cannot be modified.");
    }
}
