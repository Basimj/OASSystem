using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CostCenters.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CostCenters;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Queries.GetCostCenterById;

public sealed class GetCostCenterByIdQueryHandler(
    IReadRepository<CostCenter, Guid> repository,
    CostCenterMapper mapper)
    : IRequestHandler<GetCostCenterByIdQuery, CostCenterDto>
{
    public async Task<CostCenterDto> Handle(
        GetCostCenterByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(CostCenter), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
