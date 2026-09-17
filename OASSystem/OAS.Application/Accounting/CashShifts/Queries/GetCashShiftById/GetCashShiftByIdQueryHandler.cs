using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.CashShifts.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashShifts;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashShifts.Queries.GetCashShiftById;

public sealed class GetCashShiftByIdQueryHandler(
    IReadRepository<CashShift, Guid> repository,
    CashShiftMapper mapper)
    : IRequestHandler<GetCashShiftByIdQuery, CashShiftDto>
{
    public async Task<CashShiftDto> Handle(
        GetCashShiftByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(CashShift), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
