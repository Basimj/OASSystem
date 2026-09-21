using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.FiscalYears.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Commands.UpdateFiscalYear;

public sealed class UpdateFiscalYearCommandHandler(
    IRepository<FiscalYear, Guid> repository,
    FiscalYearMapper mapper)
    : IRequestHandler<UpdateFiscalYearCommand, FiscalYear>
{
    public async Task<FiscalYear> Handle(
        UpdateFiscalYearCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(FiscalYear), request.Id);
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
