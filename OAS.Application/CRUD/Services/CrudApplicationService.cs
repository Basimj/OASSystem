using MediatR;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.CRUD.Commands;
using OAS.Application.CRUD.Mapping;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Services;

public sealed class CrudApplicationService<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>(
    ISender sender,
    ICrudMapper<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto> mapper)
    : ICrudApplicationService<TKey, TReadDto, TCreateDto, TUpdateDto>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    public Task<PagedResult<TReadDto>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new GetEntityPageQuery<TEntity, TKey, TReadDto>(request), cancellationToken);

    public Task<TReadDto> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        sender.Send(new GetEntityByIdQuery<TEntity, TKey, TReadDto>(id), cancellationToken);

    public async Task<TReadDto> CreateAsync(TCreateDto request, CancellationToken cancellationToken = default)
    {
        var entity = await sender.Send(new CreateEntityCommand<TEntity, TKey, TCreateDto>(request), cancellationToken);
        return mapper.ToRead(entity);
    }

    public async Task<TReadDto> UpdateAsync(TKey id, TUpdateDto request, CancellationToken cancellationToken = default)
    {
        var entity = await sender.Send(new UpdateEntityCommand<TEntity, TKey, TUpdateDto>(id, request), cancellationToken);
        return mapper.ToRead(entity);
    }

    public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default) =>
        await sender.Send(new DeleteEntityCommand<TEntity, TKey>(id), cancellationToken);
}
