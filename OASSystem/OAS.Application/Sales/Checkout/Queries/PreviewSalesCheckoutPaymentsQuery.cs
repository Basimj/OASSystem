using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Checkout;

namespace OAS.Application.Sales.Checkout.Queries;

public sealed record PreviewSalesCheckoutPaymentsQuery(Guid OrderId, IReadOnlyList<CheckoutPaymentLineRequest> Payments)
    : IQuery<SalesCheckoutPaymentPreviewDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Checkout.Create];
}

public sealed class PreviewSalesCheckoutPaymentsQueryHandler(
    ICustomerOrderAggregateRepository orders,
    ISalesCheckoutContextService contextService,
    ISalesSettlementService settlement)
    : IRequestHandler<PreviewSalesCheckoutPaymentsQuery, SalesCheckoutPaymentPreviewDto>
{
    public async Task<SalesCheckoutPaymentPreviewDto> Handle(PreviewSalesCheckoutPaymentsQuery request, CancellationToken ct)
    {
        var order = await orders.GetAggregateAsync(request.OrderId, false, ct)
            ?? throw new OAS.Application.Common.Exceptions.NotFoundException("CustomerOrder", request.OrderId);
        var context = await contextService.GetAsync(request.OrderId, ct);
        var required = await settlement.CalculateOrderBaseAmountAsync(order,
            Math.Max(0m, context.PaymentSummary.OutstandingAmount), ct);
        var paid = await settlement.CalculateOrderPaymentTargetBaseAmountAsync(order, request.Payments, ct);
        var paidInOrder = order.ExchangeRate <= 0m ? 0m
            : Math.Round(paid / order.ExchangeRate, order.CurrencyDecimalPlacesSnapshot, MidpointRounding.AwayFromZero);
        var remaining = order.ExchangeRate <= 0m ? context.PaymentSummary.OutstandingAmount
            : Math.Max(0m, Math.Round((required - paid) / order.ExchangeRate, order.CurrencyDecimalPlacesSnapshot, MidpointRounding.AwayFromZero));
        return new SalesCheckoutPaymentPreviewDto(paid, required, paidInOrder, remaining);
    }
}
