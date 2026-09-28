using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class SalesPriceOverride : AuditableEntity<Guid>
{
    private SalesPriceOverride() { }

    private SalesPriceOverride(
        Guid id, Guid salesInvoiceId, Guid salesInvoiceLineId, decimal originalPrice, decimal overridePrice,
        string reason, string requestedBy, DateTimeOffset requestedAtUtc)
    {
        Id = SalesDomainGuard.Required(id, "Price override id");
        SalesInvoiceId = SalesDomainGuard.Required(salesInvoiceId, "Sales invoice id");
        SalesInvoiceLineId = SalesDomainGuard.Required(salesInvoiceLineId, "Sales invoice line id");
        if (originalPrice < 0 || overridePrice < 0)
            throw new DomainException("Sales prices cannot be negative.");
        if (originalPrice == overridePrice)
            throw new DomainException("Override price must differ from the original price.");

        OriginalPrice = originalPrice;
        OverridePrice = overridePrice;
        Reason = SalesDomainGuard.Required(reason, 1000, "Price override reason");
        RequestedBy = SalesDomainGuard.Required(requestedBy, 64, "Requested by");
        RequestedAtUtc = requestedAtUtc;
        Status = SalesPriceOverrideStatus.Pending;
        IsActive = true;
    }

    public Guid SalesInvoiceId { get; private set; }
    public Guid SalesInvoiceLineId { get; private set; }
    public decimal OriginalPrice { get; private set; }
    public decimal OverridePrice { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public SalesPriceOverrideStatus Status { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static SalesPriceOverride Request(
        Guid id, Guid salesInvoiceId, Guid salesInvoiceLineId, decimal originalPrice, decimal overridePrice,
        string reason, string requestedBy, DateTimeOffset requestedAtUtc) =>
        new(id, salesInvoiceId, salesInvoiceLineId, originalPrice, overridePrice, reason, requestedBy, requestedAtUtc);

    public void Approve(string approvedBy, DateTimeOffset approvedAtUtc)
    {
        EnsurePending();
        ApprovedBy = SalesDomainGuard.Required(approvedBy, 64, "Approved by");
        ApprovedAtUtc = approvedAtUtc;
        Status = SalesPriceOverrideStatus.Approved;
    }

    public void Reject()
    {
        EnsurePending();
        Status = SalesPriceOverrideStatus.Rejected;
        IsActive = false;
    }

    public void Cancel()
    {
        if (Status is SalesPriceOverrideStatus.Rejected or SalesPriceOverrideStatus.Cancelled)
            throw new DomainException("Price override cannot be cancelled in the current state.");
        Status = SalesPriceOverrideStatus.Cancelled;
        IsActive = false;
    }

    public bool IsApprovedFor(decimal actualUnitPrice) =>
        Status == SalesPriceOverrideStatus.Approved && IsActive && OverridePrice == actualUnitPrice;

    private void EnsurePending()
    {
        if (Status != SalesPriceOverrideStatus.Pending)
            throw new DomainException("Only pending price overrides can be approved or rejected.");
    }
}
