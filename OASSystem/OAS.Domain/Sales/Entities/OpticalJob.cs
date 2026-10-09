using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class OpticalJob : AuditableEntity<Guid>
{
    private readonly List<OpticalJobLine> _lines = [];

    private OpticalJob() { }

    private OpticalJob(
        Guid id,
        string jobCode,
        Guid customerOrderId,
        Guid? salesInvoiceId,
        Guid customerId,
        DateOnly? requiredDate,
        string? notes)
    {
        Id = SalesDomainGuard.Required(id, "Optical job id");
        JobCode = SalesDomainGuard.Required(jobCode, 40, "Optical job code");
        CustomerOrderId = SalesDomainGuard.Required(customerOrderId, "Customer order id");
        CustomerId = SalesDomainGuard.Required(customerId, "Customer id");
        if (salesInvoiceId == Guid.Empty)
            throw new DomainException("Sales invoice id cannot be empty.");

        SalesInvoiceId = salesInvoiceId;
        RequiredDate = requiredDate;
        Notes = SalesDomainGuard.Optional(notes, 1000, "Optical job notes");
        Status = OpticalJobStatus.Approved;
        IsActive = true;
    }

    public string JobCode { get; private set; } = string.Empty;
    public Guid CustomerOrderId { get; private set; }
    public Guid? SalesInvoiceId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateOnly? RequiredDate { get; private set; }
    public OpticalJobStatus Status { get; private set; }
    public Guid? AssignedTechnicianId { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<OpticalJobLine> Lines => _lines.AsReadOnly();

    public static OpticalJob Create(
        Guid id,
        string jobCode,
        Guid customerOrderId,
        Guid? salesInvoiceId,
        Guid customerId,
        DateOnly? requiredDate = null,
        string? notes = null) =>
        new(id, jobCode, customerOrderId, salesInvoiceId, customerId, requiredDate, notes);

    public void AddLine(OpticalJobLine line)
    {
        EnsurePlanningEditable();
        ArgumentNullException.ThrowIfNull(line);
        if (line.OpticalJobId != Id)
            throw new DomainException("Optical job line does not belong to this job.");
        if (_lines.Any(x => x.Id == line.Id || x.LineNumber == line.LineNumber))
            throw new DomainException("Duplicate optical job line is not allowed.");
        if (_lines.Any(x => x.CustomerOrderLineId == line.CustomerOrderLineId))
            throw new DomainException("A customer order line can only appear once in an optical job.");

        _lines.Add(line);
    }

    public void AssignTechnician(Guid? technicianId)
    {
        EnsureOpen();
        if (technicianId == Guid.Empty)
            throw new DomainException("Assigned technician id cannot be empty.");
        AssignedTechnicianId = technicianId;
    }

    public void UpdatePlanning(DateOnly? requiredDate, string? notes)
    {
        EnsurePlanningEditable();
        RequiredDate = requiredDate;
        Notes = SalesDomainGuard.Optional(notes, 1000, "Optical job notes");
    }

    public void LinkSalesInvoice(Guid salesInvoiceId)
    {
        EnsureOpen();
        SalesInvoiceId = SalesDomainGuard.Required(salesInvoiceId, "Sales invoice id");
    }

    public void MarkAwaitingMaterials()
    {
        EnsureStatus(OpticalJobStatus.Approved);
        Status = OpticalJobStatus.AwaitingMaterials;
    }

    public void MarkMaterialsAvailable()
    {
        if (Status is not (OpticalJobStatus.Approved or OpticalJobStatus.AwaitingMaterials))
            throw new DomainException("Materials can only become available from approved or awaiting-materials status.");
        if (_lines.Count == 0)
            throw new DomainException("Optical job must contain at least one line before materials can be marked available.");
        Status = OpticalJobStatus.MaterialsAvailable;
    }

    public void MarkMaterialsIssued()
    {
        EnsureStatus(OpticalJobStatus.MaterialsAvailable);
        Status = OpticalJobStatus.MaterialsIssued;
    }

    public void Start(DateTimeOffset atUtc)
    {
        if (Status is not (OpticalJobStatus.MaterialsAvailable or OpticalJobStatus.MaterialsIssued))
            throw new DomainException("Optical job can only start after all required materials are available.");
        Status = OpticalJobStatus.InProduction;
        StartedAtUtc = atUtc;
    }

    public void SendToQualityControl()
    {
        EnsureStatus(OpticalJobStatus.InProduction);
        Status = OpticalJobStatus.AwaitingQC;
    }

    public void PassQualityControl()
    {
        EnsureStatus(OpticalJobStatus.AwaitingQC);
        Status = OpticalJobStatus.QCPassed;
    }

    public void MarkReadyForDelivery(DateTimeOffset completedAtUtc)
    {
        if (Status != OpticalJobStatus.QCPassed)
            throw new DomainException("Optical job can only become ready for delivery after quality control passes.");
        Status = OpticalJobStatus.ReadyForDelivery;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkDelivered()
    {
        EnsureStatus(OpticalJobStatus.ReadyForDelivery);
        Status = OpticalJobStatus.Delivered;
        IsActive = false;
    }

    public void Cancel()
    {
        if (Status is OpticalJobStatus.Delivered or OpticalJobStatus.Cancelled)
            throw new DomainException("Delivered or cancelled optical jobs cannot be cancelled.");
        Status = OpticalJobStatus.Cancelled;
        IsActive = false;
    }

    private void EnsurePlanningEditable()
    {
        if (Status is not (OpticalJobStatus.Approved or OpticalJobStatus.AwaitingMaterials))
            throw new DomainException("Optical job planning can only be changed before materials are available.");
    }

    private void EnsureOpen()
    {
        if (!IsActive || Status is OpticalJobStatus.Delivered or OpticalJobStatus.Cancelled)
            throw new DomainException("Optical job is closed.");
    }

    private void EnsureStatus(OpticalJobStatus expected)
    {
        if (Status != expected)
            throw new DomainException($"Optical job must be {expected} for this operation.");
    }
}
