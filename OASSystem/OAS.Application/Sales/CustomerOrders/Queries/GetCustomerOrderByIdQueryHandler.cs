using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders.Queries;

public sealed class GetCustomerOrderByIdQueryHandler(ICustomerOrderAggregateRepository repository, SalesDtoAssembler assembler)
    : IRequestHandler<GetCustomerOrderByIdQuery, CustomerOrderDto>
{
    public async Task<CustomerOrderDto> Handle(GetCustomerOrderByIdQuery request, CancellationToken ct)
    {
        var entity = await repository.GetAggregateAsync(request.Id, false, ct) ?? throw new NotFoundException(nameof(CustomerOrder), request.Id);
        return await assembler.OrderAsync(entity, ct);
    }
}
