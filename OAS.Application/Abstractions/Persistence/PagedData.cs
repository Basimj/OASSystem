namespace OAS.Application.Abstractions.Persistence;

public sealed record PagedData<T>(IReadOnlyList<T> Items, long TotalCount);
