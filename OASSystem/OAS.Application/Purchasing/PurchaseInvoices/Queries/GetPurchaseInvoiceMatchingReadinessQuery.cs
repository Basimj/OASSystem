using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.PurchaseInvoices.Services;
using OAS.Contracts.Purchasing.PurchaseInvoices;

namespace OAS.Application.Purchasing.PurchaseInvoices.Queries;

public sealed record GetPurchaseInvoiceMatchingReadinessQuery(Guid Id)
    : IQuery<PurchaseInvoiceMatchingReadinessDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Invoices.View];
}

public sealed class GetPurchaseInvoiceMatchingReadinessQueryHandler(IPurchaseInvoiceRepository invoices)
    : IRequestHandler<GetPurchaseInvoiceMatchingReadinessQuery, PurchaseInvoiceMatchingReadinessDto>
{
    public async Task<PurchaseInvoiceMatchingReadinessDto> Handle(
        GetPurchaseInvoiceMatchingReadinessQuery query, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdAsync(query.Id, ct)
            ?? throw new NotFoundException("PurchaseInvoice", query.Id);
        var allocations = await invoices.GetAllocationsAsync(query.Id, ct);
        var details = allocations.Select(x => new PurchaseInvoiceReceiptAllocationDto(
            x.Id, x.PurchaseInvoiceLineId, x.PurchaseReceiptLineId,
            null, x.MatchedQuantity, x.MatchedNetAmount, x.QuantityVariance,
            x.PriceVarianceAmount, x.TaxVarianceAmount,
            (OAS.Contracts.Purchasing.Enums.PurchaseMatchStatus)(byte)x.MatchStatus,
            x.ApprovalReason, x.ApprovedBy, x.ApprovedAt,
            OAS.Application.Purchasing.Common.PurchasingRowVersion.Encode(x.RowVersion))).ToArray();
        return PurchaseInvoiceMatchingReadiness.Evaluate(invoice.Status, details);
    }
}
