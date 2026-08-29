using Mapster;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Mapping;

public sealed class ConventionCrudMapper<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>
    : ICrudMapper<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    public TEntity Create(TCreateDto source) => source.Adapt<TEntity>();

    public void Update(TUpdateDto source, TEntity destination) => source.Adapt(destination);

    public TReadDto ToRead(TEntity source) => source.Adapt<TReadDto>();
}
