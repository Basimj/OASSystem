using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.PaymentAllocations.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PaymentAllocations;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PaymentAllocations.Queries.GetPaymentAllocationById;

public sealed class GetPaymentAllocationByIdQueryHandler(
    IReadRepository<PaymentAllocation, Guid> repository,
    PaymentAllocationMapper mapper)
    : IRequestHandler<GetPaymentAllocationByIdQuery, PaymentAllocationDto>
{
    public async Task<PaymentAllocationDto> Handle(
        GetPaymentAllocationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(PaymentAllocation), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
