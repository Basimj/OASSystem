using OAS.Domain.Common.Interfaces;

namespace OAS.Domain.Common.Entities;

public abstract class SoftDeletableEntity<TKey> : AuditableEntity<TKey>, ISoftDeletable where TKey : notnull
{
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public void MarkDeleted(DateTimeOffset timestampUtc, string? userId)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = timestampUtc;
        DeletedBy = userId;
    }
}
