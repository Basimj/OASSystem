using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed class ConfirmCustomerOrderCommandHandler(
    ICustomerOrderAggregateRepository repository,
    ICustomerOrderConfirmationService confirmation,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler) : IRequestHandler<ConfirmCustomerOrderCommand, CustomerOrderDto>
{
    public async Task<CustomerOrderDto> Handle(ConfirmCustomerOrderCommand request, CancellationToken ct)
    {
        var order = await repository.GetAggregateAsync(request.OrderId, true, ct)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.OrderId);
        SalesConcurrency.Ensure(request.Request.RowVersion, order.RowVersion, "طلب العميل");

        await confirmation.ConfirmAsync(order, cancellationToken: ct);
        repository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.OrderAsync(order, ct);
    }
}
