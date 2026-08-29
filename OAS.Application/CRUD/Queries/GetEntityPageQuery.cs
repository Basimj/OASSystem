using OAS.Application.Abstractions.Messaging;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Queries;

public sealed record GetEntityPageQuery<TEntity, TKey, TReadDto>(PageRequest Request) : IQuery<PagedResult<TReadDto>>
    where TEntity : Entity<TKey> where TKey : notnull;
