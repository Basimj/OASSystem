namespace OAS.Domain.Common.Entities;

public abstract class AggregateRoot<TKey> : Entity<TKey> where TKey : notnull;
