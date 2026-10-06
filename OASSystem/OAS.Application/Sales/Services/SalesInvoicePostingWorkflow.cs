using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class SalesInvoicePostingWorkflow(
    IReadRepository<Customer, Guid> customers,
    ISalesPostingPeriodService periods,
    ISalesStockReservationService stock,
    ISalesCreditExposureService credit,
    ISalesInventoryPostingService inventory,
    ISalesInvoiceAccountingPostingService accounting,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ISalesInvoicePostingWorkflow
{
    public async Task<SalesInvoicePostingOutcome> PostAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        if (invoice.Status == SalesInvoiceStatus.Posted)
            return new SalesInvoicePostingOutcome(
                invoice.JournalEntryId ?? throw new ConflictException(SalesErrorCodes.InvoiceAlreadyPosted, "الفاتورة مرحلة بدون مرجع قيد صالح."),
                []);
        if (invoice.Status != SalesInvoiceStatus.Confirmed)
            throw new ConflictException(SalesErrorCodes.InvoiceInvalidStatus, "يجب أن تكون الفاتورة مؤكدة قبل الترحيل.");
        if (!Guid.TryParse(currentUser.UserId, out var userId))
            throw new ForbiddenException();

        var requiresInventory = invoice.Lines.Any(x => x.IsActive && x.RequiresInventory);
        await periods.GetOpenPostingPeriodAsync(invoice.PostingDate, requiresInventory, cancellationToken);

        var customer = await customers.GetByIdAsync(invoice.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), invoice.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException(SalesErrorCodes.CustomerInactive, "العميل غير فعال.");

        await credit.ValidateAsync(customer, invoice, cancellationToken);
        await stock.ValidateInvoiceReservationsAsync(invoice, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var inventoryResult = await inventory.PostAsync(invoice, currentUser.UserId!, now, cancellationToken);
        foreach (var cost in inventoryResult.LineCosts)
            invoice.SetLineCostSnapshot(cost.SalesInvoiceLineId, cost.UnitCost, cost.TotalCost);

        var journalId = await accounting.PostAsync(invoice, userId, now.UtcDateTime, cancellationToken);
        invoice.SetJournalEntry(journalId);
        invoice.Post(now, currentUser.UserId);

        // Allocation validation reads the posted invoice back through the repository. SaveChanges
        // remains inside the outer command transaction, so later settlement failures still rollback.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SalesInvoicePostingOutcome(journalId, inventoryResult.InventoryTransactionIds);
    }
}
