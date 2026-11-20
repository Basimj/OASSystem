using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.CustomerOrders.Commands;

public sealed record SetCustomerOrderHeaderDiscountCommand(Guid Id, SetCustomerOrderHeaderDiscountRequest Data)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Discount];
}

public sealed record ApproveCustomerOrderHeaderDiscountCommand(Guid Id, ApproveCustomerOrderHeaderDiscountRequest Data)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.DiscountOverride];
}

public sealed class SetCustomerOrderHeaderDiscountCommandHandler(
    ICustomerOrderAggregateRepository orders,
    LinkedSalesDraftSynchronizer linkedDrafts,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetCustomerOrderHeaderDiscountCommand, Guid>
{
    public async Task<Guid> Handle(SetCustomerOrderHeaderDiscountCommand request, CancellationToken ct)
    {
        var order = await orders.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.Id);
        SalesConcurrency.Ensure(request.Data.RowVersion, order.RowVersion, "طلب العميل");
        if (order.Status != CustomerOrderStatus.Draft)
            throw new ConflictException("sales_order_header_discount_locked", "يمكن تعديل خصم رأس الطلب في المسودة فقط.");
        var invoice = await linkedDrafts.FindLinkedInvoiceAsync(order.Id, ct);
        if (invoice is not null) LinkedSalesDraftSynchronizer.EnsureEditable(order, invoice);
        var discountType = (SalesDiscountType)(byte)request.Data.DiscountType;
        order.SetHeaderDiscount(discountType, request.Data.DiscountValue, request.Data.Reason);
        invoice?.SetHeaderDiscount(discountType, request.Data.DiscountValue, request.Data.Reason);
        await unitOfWork.SaveChangesAsync(ct);
        return order.Id;
    }
}

public sealed class ApproveCustomerOrderHeaderDiscountCommandHandler(
    ICustomerOrderAggregateRepository orders,
    LinkedSalesDraftSynchronizer linkedDrafts,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ApproveCustomerOrderHeaderDiscountCommand, Guid>
{
    public async Task<Guid> Handle(ApproveCustomerOrderHeaderDiscountCommand request, CancellationToken ct)
    {
        var order = await orders.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.Id);
        SalesConcurrency.Ensure(request.Data.RowVersion, order.RowVersion, "طلب العميل");
        var invoice = await linkedDrafts.FindLinkedInvoiceAsync(order.Id, ct);
        if (invoice is not null) LinkedSalesDraftSynchronizer.EnsureEditable(order, invoice);
        var user = currentUser.UserId ?? throw new ForbiddenException();
        var now = timeProvider.GetUtcNow();
        order.ApproveHeaderDiscount(now, user);
        invoice?.ApproveHeaderDiscount(now, user);
        await unitOfWork.SaveChangesAsync(ct);
        return order.Id;
    }
}
