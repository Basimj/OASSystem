using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Mapping;
using OAS.Application.CRUD.Queries;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Handlers;

public sealed class GetEntityByIdQueryHandler<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>(
    IReadRepository<TEntity, TKey> repository,
    ICrudMapper<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto> mapper)
    : IRequestHandler<GetEntityByIdQuery<TEntity, TKey, TReadDto>, TReadDto>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    public async Task<TReadDto> Handle(GetEntityByIdQuery<TEntity, TKey, TReadDto> request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(typeof(TEntity).Name, request.Id);
        return mapper.ToRead(entity);
    }
}
