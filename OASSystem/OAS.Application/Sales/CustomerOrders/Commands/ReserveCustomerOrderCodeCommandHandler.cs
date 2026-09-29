using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Sales.Common;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed class ReserveCustomerOrderCodeCommandHandler(ISequenceNumberGenerator sequences, IReadRepository<CustomerOrder, Guid> repository)
    : IRequestHandler<ReserveCustomerOrderCodeCommand, SalesCodeReservationDto>
{
    public async Task<SalesCodeReservationDto> Handle(ReserveCustomerOrderCodeCommand request, CancellationToken ct)
    {
        for (var i = 0; i < 100; i++)
        {
            var code = CustomerOrderCodeFormatter.Format(await sequences.NextAsync("CustomerOrderCodeSequence", ct), request.OrderDate);
            if (await repository.CountAsync(new Specification<CustomerOrder>().Where(x => x.OrderCode == code), ct) == 0)
                return new SalesCodeReservationDto(code);
        }
        throw new InvalidOperationException("Unable to reserve a unique customer order code.");
    }
}
