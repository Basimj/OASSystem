namespace OAS.Domain.Common.Events;

public abstract record DomainEvent : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
