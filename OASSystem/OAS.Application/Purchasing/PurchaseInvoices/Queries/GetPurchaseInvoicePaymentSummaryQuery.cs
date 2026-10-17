using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.PurchaseInvoices.Queries;

public sealed record GetPurchaseInvoicePaymentSummaryQuery(Guid Id)
    : IQuery<PurchaseInvoicePaymentSummaryDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Invoices.View];
}

public sealed class GetPurchaseInvoicePaymentSummaryQueryHandler(
    IReadRepository<PurchaseInvoice, Guid> invoices,
    IPurchaseInvoiceBalanceService balances)
    : IRequestHandler<GetPurchaseInvoicePaymentSummaryQuery, PurchaseInvoicePaymentSummaryDto>
{
    public async Task<PurchaseInvoicePaymentSummaryDto> Handle(GetPurchaseInvoicePaymentSummaryQuery request, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), request.Id);

        if (invoice.Status != PurchaseInvoiceStatus.Posted)
            return new(invoice.Id, invoice.TotalAmount, 0m, invoice.TotalAmount, "NotPosted", 0m, 0m);

        var balance = await balances.GetAsync(invoice, null, ct);
        var status = balance.OutstandingAmount <= 0m
            ? "Paid"
            : balance.SettledAmount > 0m
                ? "PartiallyPaid"
                : "Unpaid";

        return new PurchaseInvoicePaymentSummaryDto(
            invoice.Id,
            invoice.TotalAmount,
            balance.PaidAmount,
            balance.OutstandingAmount,
            status,
            balance.AppliedCreditInvoiceCurrencyAmount,
            balance.SettledAmount);
    }
}
