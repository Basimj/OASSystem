using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class OpticalQualityCheck : AuditableEntity<Guid>
{
    private readonly List<OpticalQualityCheckItem> _items = [];
    private OpticalQualityCheck() { }

    private OpticalQualityCheck(Guid id, Guid opticalJobId, int attemptNumber)
    {
        Id = SalesDomainGuard.Required(id, "Optical quality check id");
        OpticalJobId = SalesDomainGuard.Required(opticalJobId, "Optical job id");
        if (attemptNumber <= 0) throw new DomainException("Quality-control attempt number must be greater than zero.");
        AttemptNumber = attemptNumber;
        Result = OpticalQualityCheckResult.Pending;
    }

    public Guid OpticalJobId { get; private set; }
    public int AttemptNumber { get; private set; }
    public OpticalQualityCheckResult Result { get; private set; }
    public OpticalQcFailureAction? FailureAction { get; private set; }
    public string? GeneralNotes { get; private set; }
    public Guid? CheckedBy { get; private set; }
    public DateTimeOffset? CheckedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<OpticalQualityCheckItem> Items => _items.AsReadOnly();

    public static OpticalQualityCheck Create(Guid id, Guid opticalJobId, int attemptNumber) => new(id, opticalJobId, attemptNumber);

    public void AddItem(OpticalQualityCheckItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Result != OpticalQualityCheckResult.Pending) throw new DomainException("Completed quality checks cannot be edited.");
        if (item.QualityCheckId != Id) throw new DomainException("Quality-check item does not belong to this check.");
        if (_items.Any(x => string.Equals(x.CheckCode, item.CheckCode, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("Duplicate quality-check item is not allowed.");
        _items.Add(item);
    }

    public void CompletePassed(Guid checkedBy, DateTimeOffset atUtc, bool allItemsPassed, string? notes = null)
    {
        EnsurePending();
        if (!allItemsPassed)
            throw new DomainException("All quality-check items must be Pass or NotApplicable before passing QC.");
        CheckedBy = SalesDomainGuard.Required(checkedBy, "Checked by");
        GeneralNotes = SalesDomainGuard.Optional(notes, 2000, "Quality-control notes");
        Result = OpticalQualityCheckResult.Passed;
        CheckedAtUtc = atUtc;
        FailureAction = null;
    }

    public void CompleteFailed(Guid checkedBy, DateTimeOffset atUtc, OpticalQcFailureAction action, string reason, bool hasFailedItem)
    {
        EnsurePending();
        SalesDomainGuard.Defined(action, "QC failure action");
        if (!hasFailedItem)
            throw new DomainException("A failed quality check must contain at least one failed item.");
        CheckedBy = SalesDomainGuard.Required(checkedBy, "Checked by");
        GeneralNotes = SalesDomainGuard.Required(reason, 2000, "QC failure reason");
        FailureAction = action;
        Result = OpticalQualityCheckResult.Failed;
        CheckedAtUtc = atUtc;
    }

    private void EnsurePending()
    {
        if (Result != OpticalQualityCheckResult.Pending) throw new DomainException("Quality check is already completed.");
    }
}
