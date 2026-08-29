using OAS.Application.Abstractions.Messaging;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Queries;

public sealed record GetEntityByIdQuery<TEntity, TKey, TReadDto>(TKey Id) : IQuery<TReadDto>
    where TEntity : Entity<TKey> where TKey : notnull;
