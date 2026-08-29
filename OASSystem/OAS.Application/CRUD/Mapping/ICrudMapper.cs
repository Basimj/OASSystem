using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Mapping;

public interface ICrudMapper<TEntity, TKey, TReadDto, TCreateDto, TUpdateDto>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    TEntity Create(TCreateDto source);
    void Update(TUpdateDto source, TEntity destination);
    TReadDto ToRead(TEntity source);
}
