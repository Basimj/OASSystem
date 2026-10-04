using Microsoft.EntityFrameworkCore;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Services;

public sealed class PurchaseReturnDataPort(OasDbContext db) : IPurchaseReturnDataPort
{
    public Task<bool> HasPostedInvoiceAllocationAsync(Guid purchaseReceiptLineId, CancellationToken cancellationToken = default) =>
        (from allocation in db.Set<PurchaseInvoiceReceiptAllocation>().AsNoTracking()
         join invoiceLine in db.Set<PurchaseInvoiceLine>().AsNoTracking() on allocation.PurchaseInvoiceLineId equals invoiceLine.Id
         join invoice in db.Set<PurchaseInvoice>().AsNoTracking() on invoiceLine.PurchaseInvoiceId equals invoice.Id
         where allocation.PurchaseReceiptLineId == purchaseReceiptLineId && invoice.Status == PurchaseInvoiceStatus.Posted
         select invoice.Id).AnyAsync(cancellationToken);
}
