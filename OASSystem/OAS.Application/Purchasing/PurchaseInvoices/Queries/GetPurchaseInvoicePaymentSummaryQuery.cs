using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
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
    IReadRepository<PaymentAllocation, Guid> allocations)
    : IRequestHandler<GetPurchaseInvoicePaymentSummaryQuery, PurchaseInvoicePaymentSummaryDto>
{
    public async Task<PurchaseInvoicePaymentSummaryDto> Handle(GetPurchaseInvoicePaymentSummaryQuery request, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), request.Id);

        if (invoice.Status != PurchaseInvoiceStatus.Posted)
            return new(invoice.Id, invoice.TotalAmount, 0m, invoice.TotalAmount, "NotPosted");

        var rows = await allocations.ListAsync(
            new Specification<PaymentAllocation>().Where(x =>
                x.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice &&
                x.TargetDocumentId == invoice.Id), ct);

        var allocatedTargetBase = rows.Sum(x =>
            x.TargetBaseAllocatedAmount ??
            x.BaseAllocatedAmount ??
            (x.ExchangeRate.HasValue ? Math.Round(x.AllocatedAmount * x.ExchangeRate.Value, 4, MidpointRounding.AwayFromZero) : x.AllocatedAmount));

        var baseOutstanding = Math.Max(0m, invoice.BaseTotalAmount - allocatedTargetBase);
        var outstanding = invoice.ExchangeRate > 0m
            ? Math.Min(invoice.TotalAmount, Math.Round(baseOutstanding / invoice.ExchangeRate, 4, MidpointRounding.AwayFromZero))
            : invoice.TotalAmount;
        var paid = Math.Max(0m, invoice.TotalAmount - outstanding);
        var status = outstanding <= 0m ? "Paid" : paid > 0m ? "PartiallyPaid" : "Unpaid";

        return new(invoice.Id, invoice.TotalAmount, paid, outstanding, status);
    }
}
