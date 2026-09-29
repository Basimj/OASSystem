using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Mapping;
using OAS.Application.CRUD.Services;
using OAS.Application.Sales.CustomerOrders.Commands;
using OAS.Application.Sales.CustomerOrders.Queries;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders;

public sealed class CustomerOrderApplicationService(
    ISender sender,
    ICrudMapper<CustomerOrder, Guid, CustomerOrderDto, CreateCustomerOrderRequest, UpdateCustomerOrderRequest> mapper)
    : CrudApplicationService<CustomerOrder, Guid, CustomerOrderDto, CreateCustomerOrderRequest, UpdateCustomerOrderRequest>(sender, mapper)
{
    public override Task<PagedResult<CustomerOrderDto>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new GetCustomerOrdersQuery(request), cancellationToken);

    public override Task<CustomerOrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        sender.Send(new GetCustomerOrderByIdQuery(id), cancellationToken);

    public override async Task<CustomerOrderDto> CreateAsync(CreateCustomerOrderRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await sender.Send(new CreateCustomerOrderCommand(request), cancellationToken);
        return await sender.Send(new GetCustomerOrderByIdQuery(entity.Id), cancellationToken);
    }

    public override async Task<CustomerOrderDto> UpdateAsync(Guid id, UpdateCustomerOrderRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await sender.Send(new UpdateCustomerOrderCommand(id, request), cancellationToken);
        return await sender.Send(new GetCustomerOrderByIdQuery(entity.Id), cancellationToken);
    }

    public override Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromException(new ConflictException("sales_order_delete_forbidden", "لا يتم حذف طلب العميل حذفًا نهائيًا؛ استخدم الإلغاء."));

    public Task<SalesCodeReservationDto> ReserveCodeAsync(DateOnly orderDate, CancellationToken cancellationToken = default) =>
        sender.Send(new ReserveCustomerOrderCodeCommand(orderDate), cancellationToken);

    public Task<CustomerOrderDto> ConfirmAsync(Guid id, ConfirmCustomerOrderRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new ConfirmCustomerOrderCommand(id, request), cancellationToken);

    public Task<CustomerOrderDto> CancelAsync(Guid id, CancelCustomerOrderRequest request, CancellationToken cancellationToken = default) =>
        sender.Send(new CancelCustomerOrderCommand(id, request), cancellationToken);
}
