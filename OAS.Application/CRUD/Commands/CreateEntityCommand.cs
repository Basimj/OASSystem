using OAS.Application.Abstractions.Messaging;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Commands;

public sealed record CreateEntityCommand<TEntity, TKey, TCreateDto>(TCreateDto Data) : ICommand<TEntity>
    where TEntity : Entity<TKey> where TKey : notnull;
