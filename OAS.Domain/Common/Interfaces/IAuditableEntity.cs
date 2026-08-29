namespace OAS.Domain.Common.Interfaces;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; }
    string? CreatedBy { get; }
    DateTimeOffset? LastModifiedAtUtc { get; }
    string? LastModifiedBy { get; }
    void SetCreatedAudit(DateTimeOffset timestampUtc, string? userId);
    void SetModifiedAudit(DateTimeOffset timestampUtc, string? userId);
}
