using OAS.Domain.Common.Events;

namespace OAS.Domain.Common.Entities;

public abstract class Entity<TKey> where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TKey Id { get; protected set; } = default!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TKey> other || other.GetType() != GetType()) return false;
        if (EqualityComparer<TKey>.Default.Equals(Id, default!) ||
            EqualityComparer<TKey>.Default.Equals(other.Id, default!)) return ReferenceEquals(this, other);
        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
