using OAS.Application.Abstractions.Messaging;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Commands;

public sealed record UpdateEntityCommand<TEntity, TKey, TUpdateDto>(TKey Id, TUpdateDto Data) : ICommand<TEntity>
    where TEntity : Entity<TKey> where TKey : notnull;
