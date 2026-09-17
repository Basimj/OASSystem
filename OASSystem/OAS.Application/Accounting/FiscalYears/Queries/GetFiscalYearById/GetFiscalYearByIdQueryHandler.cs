using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.FiscalYears.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.FiscalYears.Queries.GetFiscalYearById;

public sealed class GetFiscalYearByIdQueryHandler(
    IReadRepository<FiscalYear, Guid> repository,
    FiscalYearMapper mapper)
    : IRequestHandler<GetFiscalYearByIdQuery, FiscalYearDto>
{
    public async Task<FiscalYearDto> Handle(
        GetFiscalYearByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(FiscalYear), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
