using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CostCenters.Mapping;
using OAS.Application.Accounting.CostCenters.Services;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Commands.CreateCostCenter;

public sealed class CreateCostCenterCommandHandler(
    IRepository<CostCenter, Guid> repository,
    CostCenterMapper mapper)
    : IRequestHandler<CreateCostCenterCommand, CostCenter>
{
    public async Task<CostCenter> Handle(
        CreateCostCenterCommand request,
        CancellationToken cancellationToken)
    {
        await CostCenterHierarchyValidator.ValidateAsync(
            repository, request.Data.ParentCostCenterId, null, cancellationToken);
        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
