using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseInvoices;

namespace OAS.Application.Purchasing.PurchaseInvoices.Queries;

// Read-only preflight; the matching command still validates the quantity
// inside its transaction to protect against concurrent matches and returns.
public sealed record GetPurchaseInvoiceMatchAvailabilityQuery(
    Guid InvoiceId, IReadOnlyList<Guid> ReceiptLineIds)
    : IQuery<IReadOnlyList<PurchaseReceiptMatchAvailabilityDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Invoices.Match];
}

public sealed class GetPurchaseInvoiceMatchAvailabilityQueryHandler(
    IPurchaseInvoiceRepository invoices,
    IPurchaseMatchingDataPort receiptCandidates)
    : IRequestHandler<GetPurchaseInvoiceMatchAvailabilityQuery, IReadOnlyList<PurchaseReceiptMatchAvailabilityDto>>
{
    public async Task<IReadOnlyList<PurchaseReceiptMatchAvailabilityDto>> Handle(
        GetPurchaseInvoiceMatchAvailabilityQuery query, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdAsync(query.InvoiceId, ct)
            ?? throw new NotFoundException("PurchaseInvoice", query.InvoiceId);
        var ids = query.ReceiptLineIds.Distinct().ToArray();
        if (ids.Length > 500)
            throw new ConflictException("purchasing_match_availability_request_too_large",
                "عدد بنود الاستلام المطلوب فحصها كبير جدًا. قلّل نطاق البحث ثم أعد المحاولة.");

        var result = new List<PurchaseReceiptMatchAvailabilityDto>(ids.Length);
        foreach (var receiptLineId in ids)
        {
            var candidate = await receiptCandidates.GetReceiptCandidateAsync(receiptLineId, invoice.Id, ct);
            // A caller must not use this preflight to inspect another supplier's receipts.
            if (candidate is null || candidate.SupplierId != invoice.SupplierId ||
                candidate.CurrencyId != invoice.CurrencyId ||
                candidate.ReceiptStatus != PurchaseReceiptStatus.Posted)
                continue;
            result.Add(new(receiptLineId, Math.Max(0m, candidate.AvailableQuantity)));
        }
        return result;
    }
}
