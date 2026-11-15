using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.SalesInvoices.Queries;

public sealed record PreviewSalesInvoiceCollectionQuery(Guid InvoiceId, IReadOnlyList<CheckoutPaymentLineRequest> PaymentLines)
    : IQuery<SalesInvoiceCollectionPreviewDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.Collect];
}

public sealed class PreviewSalesInvoiceCollectionQueryHandler(
    ISalesInvoiceAggregateRepository invoices,
    ISalesInvoiceBalanceService balances,
    ISalesSettlementService settlement)
    : IRequestHandler<PreviewSalesInvoiceCollectionQuery, SalesInvoiceCollectionPreviewDto>
{
    public async Task<SalesInvoiceCollectionPreviewDto> Handle(PreviewSalesInvoiceCollectionQuery request, CancellationToken ct)
    {
        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, false, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.InvoiceId);
        if (invoice.Status != SalesInvoiceStatus.Posted)
            return new(false, 0, 0, 0, invoice.BaseCurrencyCodeSnapshot, "يجب ترحيل الفاتورة قبل تحصيل دفعة.");
        if (request.PaymentLines.Count == 0)
            return new(false, 0, 0, 0, invoice.BaseCurrencyCodeSnapshot, "أدخل دفعة واحدة على الأقل.");
        var outstanding = await balances.GetAsync(invoice, cancellationToken: ct);
        var amount = await settlement.CalculateInvoicePaymentTargetBaseAmountAsync(invoice, request.PaymentLines, ct);
        var valid = amount > 0.0001m && amount <= outstanding.OutstandingBaseAmount + 0.0001m &&
            outstanding.OutstandingBaseAmount > 0.0001m;
        return new(valid, amount, outstanding.OutstandingBaseAmount,
            Math.Max(0m, outstanding.OutstandingBaseAmount - amount),
            invoice.BaseCurrencyCodeSnapshot,
            valid ? null : outstanding.OutstandingBaseAmount <= 0.0001m ?
                "لا يوجد مبلغ متبقٍ للتحصيل؛ راجع السداد والمرتجعات." :
                "مبلغ التحصيل المحوّل يتجاوز الرصيد المتبقي أو غير صالح.");
    }
}
