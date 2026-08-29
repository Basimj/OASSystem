using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.CRUD.Commands;
using OAS.Application.CRUD.Mapping;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Handlers;

public sealed class CreateEntityCommandHandler<TEntity, TKey, TCreateDto, TReadDto, TUpdateDto>(
    IRepository<TEntity, TKey> repository,
    ICrudMapper<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto> mapper)
    : IRequestHandler<CreateEntityCommand<TEntity, TKey, TCreateDto>, TEntity>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    public async Task<TEntity> Handle(CreateEntityCommand<TEntity, TKey, TCreateDto> request, CancellationToken cancellationToken)
    {
        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
