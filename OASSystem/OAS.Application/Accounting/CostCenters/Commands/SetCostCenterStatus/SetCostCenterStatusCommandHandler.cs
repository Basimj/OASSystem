using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Commands.SetCostCenterStatus;

public sealed class SetCostCenterStatusCommandHandler(
    IRepository<CostCenter, Guid> repository)
    : IRequestHandler<SetCostCenterStatusCommand>
{
    public async Task Handle(
        SetCostCenterStatusCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(CostCenter), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The cost center has been modified by another user.");
        }

        entity.SetActive(request.Request.IsActive);
        repository.Update(entity);
    }
}
