using OAS.Domain.Common.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class OpticalJobStatusHistory : AuditableEntity<Guid>
{
    private OpticalJobStatusHistory() { }

    private OpticalJobStatusHistory(Guid id, Guid opticalJobId, OpticalJobStatus? fromStatus, OpticalJobStatus toStatus,
        string? reason, Guid changedBy, DateTimeOffset changedAtUtc, string? correlationId)
    {
        Id = SalesDomainGuard.Required(id, "Optical job history id");
        OpticalJobId = SalesDomainGuard.Required(opticalJobId, "Optical job id");
        if (fromStatus.HasValue) SalesDomainGuard.Defined(fromStatus.Value, "From status");
        SalesDomainGuard.Defined(toStatus, "To status");
        ChangedBy = SalesDomainGuard.Required(changedBy, "Changed by");
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = SalesDomainGuard.Optional(reason, 1000, "Status reason");
        ChangedAtUtc = changedAtUtc;
        CorrelationId = SalesDomainGuard.Optional(correlationId, 100, "Correlation id");
    }

    public Guid OpticalJobId { get; private set; }
    public OpticalJobStatus? FromStatus { get; private set; }
    public OpticalJobStatus ToStatus { get; private set; }
    public string? Reason { get; private set; }
    public Guid ChangedBy { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }

    public static OpticalJobStatusHistory Create(Guid id, Guid opticalJobId, OpticalJobStatus? fromStatus,
        OpticalJobStatus toStatus, string? reason, Guid changedBy, DateTimeOffset changedAtUtc, string? correlationId = null) =>
        new(id, opticalJobId, fromStatus, toStatus, reason, changedBy, changedAtUtc, correlationId);
}
