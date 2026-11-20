using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Common;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed record SetSalesInvoiceHeaderDiscountCommand(Guid Id, SetSalesInvoiceHeaderDiscountRequest Data)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Discount];
}

public sealed record ApproveSalesInvoiceHeaderDiscountCommand(Guid Id, ApproveSalesInvoiceHeaderDiscountRequest Data)
    : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.DiscountOverride];
}

public sealed class SetSalesInvoiceHeaderDiscountCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    ICustomerOrderAggregateRepository orders,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetSalesInvoiceHeaderDiscountCommand, Guid>
{
    public async Task<Guid> Handle(SetSalesInvoiceHeaderDiscountCommand request, CancellationToken ct)
    {
        var invoice = await invoices.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.Id);
        SalesConcurrency.Ensure(request.Data.RowVersion, invoice.RowVersion, "فاتورة المبيعات");
        CustomerOrder? order = null;
        if (invoice.CustomerOrderId is Guid orderId)
        {
            order = await orders.GetAggregateAsync(orderId, true, ct)
                ?? throw new NotFoundException(nameof(CustomerOrder), orderId);
            LinkedSalesDraftSynchronizer.EnsureEditable(order, invoice);
        }
        if (invoice.Status != SalesInvoiceStatus.Draft)
            throw new ConflictException("sales_header_discount_locked", "يمكن تعديل خصم رأس الفاتورة في المسودة فقط.");

        var discountType = (SalesDiscountType)(byte)request.Data.DiscountType;
        invoice.SetHeaderDiscount(discountType, request.Data.DiscountValue, request.Data.Reason);
        order?.SetHeaderDiscount(discountType, request.Data.DiscountValue, request.Data.Reason);
        await unitOfWork.SaveChangesAsync(ct);
        return invoice.Id;
    }
}

public sealed class ApproveSalesInvoiceHeaderDiscountCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    ICustomerOrderAggregateRepository orders,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ApproveSalesInvoiceHeaderDiscountCommand, Guid>
{
    public async Task<Guid> Handle(ApproveSalesInvoiceHeaderDiscountCommand request, CancellationToken ct)
    {
        var invoice = await invoices.GetAggregateAsync(request.Id, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.Id);
        SalesConcurrency.Ensure(request.Data.RowVersion, invoice.RowVersion, "فاتورة المبيعات");
        CustomerOrder? order = null;
        if (invoice.CustomerOrderId is Guid orderId)
        {
            order = await orders.GetAggregateAsync(orderId, true, ct)
                ?? throw new NotFoundException(nameof(CustomerOrder), orderId);
            LinkedSalesDraftSynchronizer.EnsureEditable(order, invoice);
        }
        var user = currentUser.UserId ?? throw new ForbiddenException();
        var now = timeProvider.GetUtcNow();
        invoice.ApproveHeaderDiscount(now, user);
        order?.ApproveHeaderDiscount(now, user);
        await unitOfWork.SaveChangesAsync(ct);
        return invoice.Id;
    }
}
