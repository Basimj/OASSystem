namespace OAS.Domain.Common.Interfaces;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAtUtc { get; }
    string? DeletedBy { get; }
    void MarkDeleted(DateTimeOffset timestampUtc, string? userId);
}
