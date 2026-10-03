using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Application.Common.Exceptions;

namespace OAS.Application.Sales.Services;

public sealed class SalesStockReservationService(
    IInventoryBalanceRepository balances,
    IRepository<StockReservation, Guid> reservations,
    TimeProvider timeProvider) : ISalesStockReservationService
{
    // V1 policy: reservation is all-or-none per inventory line. We never create a partial
    // reservation for a single line; an order becomes PartiallyAvailable when some complete
    // lines are reserved and others are not.
    public async Task<CustomerOrderStatus> ReserveForOrderAsync(CustomerOrder order, CancellationToken cancellationToken = default)
    {
        var inventoryLines = order.Lines.Where(x => x.IsActive && x.RequiresInventory).ToList();
        if (inventoryLines.Count == 0)
            return order.Lines.Any(x => x.IsActive && x.RequiresProduction)
                ? CustomerOrderStatus.ReadyForProduction
                : CustomerOrderStatus.Confirmed;

        var reservedCount = 0;
        foreach (var line in inventoryLines)
        {
            var existing = await FindActiveAsync(SalesSourceReferences.CustomerOrder, order.Id, line.Id, cancellationToken);
            if (existing.Count > 0)
            {
                if (existing.Sum(x => x.Quantity) >= line.Quantity) reservedCount++;
                continue;
            }

            var balance = await balances.GetByWarehouseAndVariantAsync(line.WarehouseId!.Value, line.ProductVariantId!.Value, cancellationToken);
            if (balance is null || !balance.TryReserve(line.Quantity))
                continue;

            balances.Update(balance);
            await reservations.AddAsync(StockReservation.Create(
                Guid.NewGuid(), line.ProductVariantId.Value, line.WarehouseId.Value, line.Quantity,
                SalesSourceReferences.Module, SalesSourceReferences.CustomerOrder, order.Id, line.Id,
                timeProvider.GetUtcNow()), cancellationToken);
            reservedCount++;
        }

        if (reservedCount == 0) return CustomerOrderStatus.AwaitingStock;
        if (reservedCount < inventoryLines.Count) return CustomerOrderStatus.PartiallyAvailable;
        return order.Lines.Any(x => x.IsActive && x.RequiresProduction)
            ? CustomerOrderStatus.ReadyForProduction
            : CustomerOrderStatus.Confirmed;
    }

    public async Task<bool> HasSufficientStockForInvoiceAsync(SalesInvoice invoice, CancellationToken cancellationToken = default)
    {
        var additionalRequired = new Dictionary<(Guid WarehouseId, Guid ProductVariantId), decimal>();

        foreach (var line in invoice.Lines.Where(x => x.IsActive && x.RequiresInventory))
        {
            if (invoice.CustomerOrderId.HasValue && line.CustomerOrderLineId.HasValue)
            {
                var orderReservations = await FindActiveAsync(
                    SalesSourceReferences.CustomerOrder,
                    invoice.CustomerOrderId.Value,
                    line.CustomerOrderLineId.Value,
                    cancellationToken);

                if (orderReservations.Sum(x => x.Quantity) < line.Quantity)
                    return false;

                continue;
            }

            var existing = await FindActiveAsync(
                SalesSourceReferences.SalesInvoice,
                invoice.Id,
                line.Id,
                cancellationToken);

            var missingQuantity = line.Quantity - existing.Sum(x => x.Quantity);
            if (missingQuantity <= 0)
                continue;

            var key = (line.WarehouseId!.Value, line.ProductVariantId!.Value);
            additionalRequired[key] = additionalRequired.GetValueOrDefault(key) + missingQuantity;
        }

        foreach (var requirement in additionalRequired)
        {
            var balance = await balances.GetByWarehouseAndVariantAsync(
                requirement.Key.WarehouseId,
                requirement.Key.ProductVariantId,
                cancellationToken);

            if (balance is null || balance.AvailableQuantity < requirement.Value)
                return false;
        }

        return true;
    }

    public async Task EnsureReservationsForInvoiceAsync(SalesInvoice invoice, CancellationToken cancellationToken = default)
    {
        foreach (var line in invoice.Lines.Where(x => x.IsActive && x.RequiresInventory))
        {
            if (invoice.CustomerOrderId.HasValue && line.CustomerOrderLineId.HasValue)
            {
                var orderReservations = await FindActiveAsync(SalesSourceReferences.CustomerOrder, invoice.CustomerOrderId.Value, line.CustomerOrderLineId.Value, cancellationToken);
                if (orderReservations.Sum(x => x.Quantity) < line.Quantity)
                    throw new ConflictException(SalesErrorCodes.InsufficientStock, "حجز المخزون الخاص بطلب العميل غير كافٍ لهذا السطر.");
                continue;
            }

            var existing = await FindActiveAsync(SalesSourceReferences.SalesInvoice, invoice.Id, line.Id, cancellationToken);
            if (existing.Sum(x => x.Quantity) >= line.Quantity)
                continue;

            var balance = await balances.GetByWarehouseAndVariantAsync(line.WarehouseId!.Value, line.ProductVariantId!.Value, cancellationToken);
            if (balance is null || !balance.TryReserve(line.Quantity))
                throw new ConflictException(SalesErrorCodes.InsufficientStock, "الكمية المتاحة غير كافية لتأكيد الفاتورة.");

            balances.Update(balance);
            await reservations.AddAsync(StockReservation.Create(
                Guid.NewGuid(), line.ProductVariantId.Value, line.WarehouseId.Value, line.Quantity,
                SalesSourceReferences.Module, SalesSourceReferences.SalesInvoice, invoice.Id, line.Id,
                timeProvider.GetUtcNow()), cancellationToken);
        }
    }

    public Task ReleaseOrderReservationsAsync(Guid orderId, DateTimeOffset releasedAtUtc, CancellationToken cancellationToken = default) =>
        ReleaseAsync(SalesSourceReferences.CustomerOrder, orderId, releasedAtUtc, cancellationToken);

    public Task ReleaseInvoiceReservationsAsync(Guid invoiceId, DateTimeOffset releasedAtUtc, CancellationToken cancellationToken = default) =>
        ReleaseAsync(SalesSourceReferences.SalesInvoice, invoiceId, releasedAtUtc, cancellationToken);

    public async Task ValidateInvoiceReservationsAsync(SalesInvoice invoice, CancellationToken cancellationToken = default)
    {
        foreach (var line in invoice.Lines.Where(x => x.IsActive && x.RequiresInventory))
        {
            IReadOnlyList<StockReservation> active;
            if (invoice.CustomerOrderId.HasValue && line.CustomerOrderLineId.HasValue)
                active = await FindActiveAsync(SalesSourceReferences.CustomerOrder, invoice.CustomerOrderId.Value, line.CustomerOrderLineId.Value, cancellationToken);
            else
                active = await FindActiveAsync(SalesSourceReferences.SalesInvoice, invoice.Id, line.Id, cancellationToken);

            if (active.Sum(x => x.Quantity) < line.Quantity)
                throw new ConflictException(SalesErrorCodes.ReservationConflict, "حجز المخزون تغير أو لم يعد كافيًا. حدّث المستند وحاول مرة أخرى.");
        }
    }

    private async Task ReleaseAsync(string documentType, Guid documentId, DateTimeOffset releasedAtUtc, CancellationToken cancellationToken)
    {
        var spec = new Specification<StockReservation>()
            .Where(x => x.SourceModule == SalesSourceReferences.Module && x.SourceDocumentType == documentType &&
                        x.SourceDocumentId == documentId && x.Status == StockReservationStatus.Active && x.IsActive)
            .Tracking();
        var active = await reservations.ListAsync(spec, cancellationToken);
        foreach (var reservation in active)
        {
            var balance = await balances.GetByWarehouseAndVariantAsync(reservation.WarehouseId, reservation.ProductVariantId, cancellationToken)
                ?? throw new ConflictException(SalesErrorCodes.ReservationConflict, "تعذر العثور على رصيد المخزون المرتبط بالحجز.");
            balance.ReleaseReservation(reservation.Quantity);
            balances.Update(balance);
            // Reservation was loaded with Tracking(); changing it is enough for the current UnitOfWork.
            reservation.Release(releasedAtUtc);
        }
    }

    private async Task<IReadOnlyList<StockReservation>> FindActiveAsync(string documentType, Guid documentId, Guid lineId, CancellationToken cancellationToken)
    {
        var spec = new Specification<StockReservation>()
            .Where(x => x.SourceModule == SalesSourceReferences.Module && x.SourceDocumentType == documentType &&
                        x.SourceDocumentId == documentId && x.SourceLineId == lineId &&
                        x.Status == StockReservationStatus.Active && x.IsActive)
            .Tracking();
        return await reservations.ListAsync(spec, cancellationToken);
    }
}
