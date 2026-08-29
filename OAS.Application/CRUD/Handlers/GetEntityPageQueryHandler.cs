using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.CRUD.Mapping;
using OAS.Application.CRUD.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Handlers;

public sealed class GetEntityPageQueryHandler<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>(
    IReadRepository<TEntity, TKey> repository,
    ICrudSpecificationFactory<TEntity> specificationFactory,
    ICrudMapper<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto> mapper)
    : IRequestHandler<GetEntityPageQuery<TEntity, TKey, TReadDto>, PagedResult<TReadDto>>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    public async Task<PagedResult<TReadDto>> Handle(GetEntityPageQuery<TEntity, TKey, TReadDto> request, CancellationToken cancellationToken)
    {
        var normalized = request.Request.Normalize();
        var spec = specificationFactory.CreatePageSpecification(normalized);
        var page = await repository.GetPageAsync(spec, cancellationToken);
        return new PagedResult<TReadDto>
        {
            Items = page.Items.Select(mapper.ToRead).ToArray(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
