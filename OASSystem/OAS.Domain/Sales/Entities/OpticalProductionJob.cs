using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Production;

namespace OAS.Domain.Sales.Entities;

public sealed class OpticalProductionJob : AuditableEntity<Guid>
{
    private readonly List<OpticalProductionMaterial> _materials = [];

    private OpticalProductionJob() { }

    private OpticalProductionJob(
        Guid id,
        string code,
        Guid invoiceId,
        Guid invoiceLineId,
        Guid customerId,
        Guid warehouseId,
        DateOnly createdDate,
        DateOnly? targetDate,
        string? notes,
        Guid? remakeOfJobId,
        int remakeNumber)
    {
        if (id == Guid.Empty || invoiceId == Guid.Empty || invoiceLineId == Guid.Empty ||
            customerId == Guid.Empty || warehouseId == Guid.Empty)
            throw new DomainException("Production job identifiers are required.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Production job code is required.");
        if (targetDate.HasValue && targetDate.Value < createdDate)
            throw new DomainException("Production target date cannot precede creation date.");
        if (remakeOfJobId == Guid.Empty)
            throw new DomainException("Remake source job id cannot be empty.");
        if (remakeNumber < 0)
            throw new DomainException("Remake number cannot be negative.");
        if (!remakeOfJobId.HasValue && remakeNumber != 0)
            throw new DomainException("A remake number requires a remake source job.");
        if (remakeOfJobId.HasValue && remakeNumber <= 0)
            throw new DomainException("A remake job must have a positive remake number.");

        Id = id;
        JobCode = code.Trim();
        SalesInvoiceId = invoiceId;
        SalesInvoiceLineId = invoiceLineId;
        CustomerId = customerId;
        WarehouseId = warehouseId;
        JobDate = createdDate;
        TargetDate = targetDate;
        Notes = Normalize(notes);
        RemakeOfJobId = remakeOfJobId;
        RemakeNumber = remakeNumber;
        Status = OpticalProductionStatus.Draft;
        IsActive = true;
    }

    public string JobCode { get; private set; } = string.Empty;
    public Guid SalesInvoiceId { get; private set; }
    public Guid SalesInvoiceLineId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateOnly JobDate { get; private set; }
    public DateOnly? TargetDate { get; private set; }
    public OpticalProductionStatus Status { get; private set; }
    public Guid? InventoryTransactionId { get; private set; }
    public decimal MaterialCostBase { get; private set; }
    public string? Notes { get; private set; }
    public Guid? RemakeOfJobId { get; private set; }
    public int RemakeNumber { get; private set; }
    public OpticalProductionQcResult? LastQcResult { get; private set; }
    public int QcAttemptCount { get; private set; }
    public int FailedQcCount { get; private set; }
    public string? LastQcNotes { get; private set; }
    public bool LastQcWasBreakage { get; private set; }
    public DateTimeOffset? ReleasedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? QcAtUtc { get; private set; }
    public DateTimeOffset? FailedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public bool IsRemake => RemakeOfJobId.HasValue;
    public IReadOnlyCollection<OpticalProductionMaterial> Materials => _materials.AsReadOnly();

    public static OpticalProductionJob Create(
        Guid id,
        string code,
        Guid invoiceId,
        Guid invoiceLineId,
        Guid customerId,
        Guid warehouseId,
        DateOnly jobDate,
        DateOnly? targetDate,
        string? notes = null,
        Guid? remakeOfJobId = null,
        int remakeNumber = 0) =>
        new(id, code, invoiceId, invoiceLineId, customerId, warehouseId, jobDate, targetDate, notes, remakeOfJobId, remakeNumber);

    public void AddMaterial(OpticalProductionMaterial material)
    {
        if (Status != OpticalProductionStatus.Draft)
            throw new DomainException("Materials can only be changed while production job is draft.");
        if (material.OpticalProductionJobId != Id)
            throw new DomainException("Production material does not belong to this job.");
        if (_materials.Any(x => x.ProductVariantId == material.ProductVariantId))
            throw new DomainException("Duplicate production material is not allowed.");
        _materials.Add(material);
    }

    public void Release(DateTimeOffset at)
    {
        if (Status != OpticalProductionStatus.Draft)
            throw new DomainException("Only draft production jobs can be released.");
        if (_materials.Count == 0)
            throw new DomainException("Production job requires at least one material.");
        Status = OpticalProductionStatus.Released;
        ReleasedAtUtc = at;
    }

    public void Start(DateTimeOffset at)
    {
        if (Status != OpticalProductionStatus.Released)
            throw new DomainException("Only released production jobs can be started.");
        Status = OpticalProductionStatus.InProgress;
        StartedAtUtc = at;
    }

    public void SetMaterialIssue(Guid transactionId)
    {
        if (Status != OpticalProductionStatus.InProgress)
            throw new DomainException("Materials can only be issued for in-progress production jobs.");
        if (InventoryTransactionId.HasValue)
            throw new DomainException("Production materials were already issued.");
        if (transactionId == Guid.Empty)
            throw new DomainException("Inventory transaction id is required.");
        if (_materials.Any(x => !x.TotalCostSnapshot.HasValue))
            throw new DomainException("All production material costs must be captured before issue.");

        InventoryTransactionId = transactionId;
        MaterialCostBase = decimal.Round(_materials.Sum(x => x.TotalCostSnapshot ?? 0m), 4);
    }

    public void SubmitQualityControl(DateTimeOffset at, bool passed, string? notes = null, bool isBreakage = false)
    {
        if (Status != OpticalProductionStatus.InProgress)
            throw new DomainException("Only in-progress jobs can enter quality control.");
        if (!InventoryTransactionId.HasValue)
            throw new DomainException("Materials must be issued before quality control.");
        if (isBreakage && passed)
            throw new DomainException("A passed quality-control result cannot be marked as breakage.");

        QcAttemptCount++;
        QcAtUtc = at;
        LastQcNotes = Normalize(notes);
        LastQcWasBreakage = isBreakage;

        if (passed)
        {
            LastQcResult = OpticalProductionQcResult.Passed;
            Status = OpticalProductionStatus.QualityControl;
            FailedAtUtc = null;
            return;
        }

        LastQcResult = OpticalProductionQcResult.Failed;
        FailedQcCount++;
        FailedAtUtc = at;
        Status = OpticalProductionStatus.QualityControlFailed;
        IsActive = false;
    }

    public void Complete(DateTimeOffset at)
    {
        if (Status != OpticalProductionStatus.QualityControl || LastQcResult != OpticalProductionQcResult.Passed)
            throw new DomainException("Only production jobs that passed quality control can be completed.");
        Status = OpticalProductionStatus.Completed;
        CompletedAtUtc = at;
        IsActive = false;
    }

    public void Cancel()
    {
        if (Status is OpticalProductionStatus.Completed or OpticalProductionStatus.Cancelled or OpticalProductionStatus.QualityControlFailed)
            throw new DomainException("Completed, failed-quality, or cancelled production jobs cannot be cancelled.");
        if (InventoryTransactionId.HasValue)
            throw new DomainException("A production job with issued materials cannot be cancelled; use quality control and a remake instead.");
        Status = OpticalProductionStatus.Cancelled;
        IsActive = false;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
