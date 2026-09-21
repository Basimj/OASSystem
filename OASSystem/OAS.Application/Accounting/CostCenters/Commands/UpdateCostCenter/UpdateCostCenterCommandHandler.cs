using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CostCenters.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Commands.UpdateCostCenter;

public sealed class UpdateCostCenterCommandHandler(
    IRepository<CostCenter, Guid> repository,
    CostCenterMapper mapper)
    : IRequestHandler<UpdateCostCenterCommand, CostCenter>
{
    public async Task<CostCenter> Handle(
        UpdateCostCenterCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(CostCenter), request.Id);
        }

        if (!string.Equals(entity.Code, request.Data.Code.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                "accounting_cost_center_code_immutable",
                "The cost center code cannot be changed after creation.");
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The cost center has been modified by another user.");
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
