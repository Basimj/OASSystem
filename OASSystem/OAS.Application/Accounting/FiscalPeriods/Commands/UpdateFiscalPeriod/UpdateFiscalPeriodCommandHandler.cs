using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.FiscalPeriods.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalPeriods.Commands.UpdateFiscalPeriod;

public sealed class UpdateFiscalPeriodCommandHandler(
    IRepository<FiscalPeriod, Guid> repository,
    FiscalPeriodMapper mapper)
    : IRequestHandler<UpdateFiscalPeriodCommand, FiscalPeriod>
{
    public async Task<FiscalPeriod> Handle(
        UpdateFiscalPeriodCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(FiscalPeriod), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The fiscal period has been modified by another user.");
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}