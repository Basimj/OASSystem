using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.FiscalPeriods.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalPeriods.Queries.GetFiscalPeriodById;

public sealed class GetFiscalPeriodByIdQueryHandler(
    IReadRepository<FiscalPeriod, Guid> repository,
    FiscalPeriodMapper mapper)
    : IRequestHandler<GetFiscalPeriodByIdQuery, FiscalPeriodDto>
{
    public async Task<FiscalPeriodDto> Handle(
        GetFiscalPeriodByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(FiscalPeriod), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
