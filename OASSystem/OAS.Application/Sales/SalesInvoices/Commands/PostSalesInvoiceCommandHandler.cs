using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.SalesInvoices.Commands;

public sealed class PostSalesInvoiceCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    IReadRepository<Customer, Guid> customers,
    ISalesPostingPeriodService periods,
    ISalesStockReservationService stock,
    ISalesCreditExposureService credit,
    ISalesInventoryPostingService inventory,
    ISalesInvoiceAccountingPostingService accounting,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<PostSalesInvoiceCommand, SalesInvoicePostingResultDto>
{
    public async Task<SalesInvoicePostingResultDto> Handle(PostSalesInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.InvoiceId);
        SalesConcurrency.Ensure(request.Request.RowVersion, invoice.RowVersion, "فاتورة المبيعات");

        if (invoice.Status == SalesInvoiceStatus.Posted)
            throw new ConflictException(SalesErrorCodes.InvoiceAlreadyPosted, "الفاتورة مرحلة مسبقًا.");
        if (invoice.Status != SalesInvoiceStatus.Confirmed)
            throw new ConflictException(SalesErrorCodes.InvoiceInvalidStatus, "يجب أن تكون الفاتورة مؤكدة قبل الترحيل.");

        if (!Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var requiresInventory = invoice.Lines.Any(x => x.IsActive && x.RequiresInventory);
        await periods.GetOpenPostingPeriodAsync(invoice.PostingDate, requiresInventory, ct);

        var customer = await customers.GetByIdAsync(invoice.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), invoice.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");

        // Credit and reservation checks are intentionally repeated at Posting inside the same
        // transaction; Confirm-time validation is not sufficient under concurrency.
        await credit.ValidateAsync(customer, invoice, ct);
        await stock.ValidateInvoiceReservationsAsync(invoice, ct);

        var now = timeProvider.GetUtcNow();
        var inventoryResult = await inventory.PostAsync(invoice, currentUser.UserId!, now, ct);
        foreach (var cost in inventoryResult.LineCosts)
            invoice.SetLineCostSnapshot(cost.SalesInvoiceLineId, cost.UnitCost, cost.TotalCost);

        var journalId = await accounting.PostAsync(invoice, userId, now.UtcDateTime, ct);
        invoice.SetJournalEntry(journalId);
        invoice.Post(now, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(ct);

        return new SalesInvoicePostingResultDto(
            invoice.Id,
            invoice.InvoiceCode,
            journalId,
            inventoryResult.InventoryTransactionIds,
            Convert.ToBase64String(invoice.RowVersion));
    }
}
