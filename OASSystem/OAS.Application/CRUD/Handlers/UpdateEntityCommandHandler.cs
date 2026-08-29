using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Commands;
using OAS.Application.CRUD.Mapping;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Handlers;

public sealed class UpdateEntityCommandHandler<TEntity, TKey, TUpdateDto, TReadDto, TCreateDto>(
    IRepository<TEntity, TKey> repository,
    ICrudMapper<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto> mapper)
    : IRequestHandler<UpdateEntityCommand<TEntity, TKey, TUpdateDto>, TEntity>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    public async Task<TEntity> Handle(UpdateEntityCommand<TEntity, TKey, TUpdateDto> request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(typeof(TEntity).Name, request.Id);
        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
