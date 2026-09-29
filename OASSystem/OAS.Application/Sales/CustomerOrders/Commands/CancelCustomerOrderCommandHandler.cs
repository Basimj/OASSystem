using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed class CancelCustomerOrderCommandHandler(
    ICustomerOrderAggregateRepository repository,
    ISalesStockReservationService stock,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    SalesDtoAssembler assembler) : IRequestHandler<CancelCustomerOrderCommand, CustomerOrderDto>
{
    public async Task<CustomerOrderDto> Handle(CancelCustomerOrderCommand request, CancellationToken ct)
    {
        var order = await repository.GetAggregateAsync(request.OrderId, true, ct) ?? throw new NotFoundException(nameof(CustomerOrder), request.OrderId);
        SalesConcurrency.Ensure(request.Request.RowVersion, order.RowVersion, "طلب العميل");
        var now = timeProvider.GetUtcNow();
        await stock.ReleaseOrderReservationsAsync(order.Id, now, ct);
        order.Cancel(now, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(ct);
        return await assembler.OrderAsync(order, ct);
    }
}
