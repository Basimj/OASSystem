using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.Enums;
using OAS.Contracts.Purchasing.PurchaseInvoices;

namespace OAS.Application.Purchasing.PurchaseInvoices.Queries;

/// <summary>Remaining unbilled quantities from a posted receipt, before an invoice exists.
/// Recomputed server-side; the matching command remains authoritative.</summary>
public sealed record GetPurchaseReceiptInvoiceAvailabilityQuery(Guid ReceiptId)
    : IQuery<IReadOnlyList<PurchaseReceiptMatchAvailabilityDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Invoices.Create];
}

public sealed class GetPurchaseReceiptInvoiceAvailabilityQueryHandler(
    IPurchaseReceiptRepository receipts, IPurchaseMatchingDataPort matching)
    : IRequestHandler<GetPurchaseReceiptInvoiceAvailabilityQuery, IReadOnlyList<PurchaseReceiptMatchAvailabilityDto>>
{
    public async Task<IReadOnlyList<PurchaseReceiptMatchAvailabilityDto>> Handle(
        GetPurchaseReceiptInvoiceAvailabilityQuery query, CancellationToken ct)
    {
        var receipt = await receipts.GetByIdAsync(query.ReceiptId, ct)
            ?? throw new NotFoundException("PurchaseReceipt", query.ReceiptId);
        if (receipt.Status != OAS.Domain.Purchasing.Enums.PurchaseReceiptStatus.Posted)
            throw new ConflictException("purchase_receipt_not_posted",
                "لا يمكن إنشاء فاتورة من استلام لم يُرحّل بعد.");

        var result = new List<PurchaseReceiptMatchAvailabilityDto>();
        foreach (var line in receipt.Lines)
        {
            // No invoice exists yet. Guid.Empty excludes none of the prior invoice allocations.
            var candidate = await matching.GetReceiptCandidateAsync(line.Id, Guid.Empty, ct);
            if (candidate is null || candidate.SupplierId != receipt.SupplierId ||
                candidate.ReceiptStatus != PurchaseReceiptStatus.Posted ||
                candidate.PurchaseOrderLineId != line.PurchaseOrderLineId)
                continue;
            result.Add(new(line.Id, Math.Max(0m, candidate.AvailableQuantity)));
        }
        return result;
    }
}
