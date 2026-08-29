using OAS.Application.Abstractions.Messaging;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Commands;

public sealed record DeleteEntityCommand<TEntity, TKey>(TKey Id) : ICommand
    where TEntity : Entity<TKey> where TKey : notnull;
