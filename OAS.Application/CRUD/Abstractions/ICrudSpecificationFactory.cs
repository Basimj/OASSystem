using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;

namespace OAS.Application.CRUD.Abstractions;

public interface ICrudSpecificationFactory<TEntity>
{
    ISpecification<TEntity> CreatePageSpecification(PageRequest request);
}
