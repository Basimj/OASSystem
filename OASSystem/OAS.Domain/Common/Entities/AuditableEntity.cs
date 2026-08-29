using OAS.Domain.Common.Interfaces;

namespace OAS.Domain.Common.Entities;

public abstract class AuditableEntity<TKey> : Entity<TKey>, IAuditableEntity where TKey : notnull
{
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedAtUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    public void SetCreatedAudit(DateTimeOffset timestampUtc, string? userId)
    {
        if (CreatedAtUtc != default) return;
        CreatedAtUtc = timestampUtc;
        CreatedBy = userId;
    }

    public void SetModifiedAudit(DateTimeOffset timestampUtc, string? userId)
    {
        LastModifiedAtUtc = timestampUtc;
        LastModifiedBy = userId;
    }
}
