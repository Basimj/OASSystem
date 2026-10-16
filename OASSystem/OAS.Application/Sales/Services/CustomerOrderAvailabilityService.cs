using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Services;

public sealed class CustomerOrderAvailabilityService(
    IInventoryBalanceRepository balances,
    IReadRepository<OAS.Domain.Entities.Inventory.StockReservation, Guid> reservations,
    IReadRepository<Warehouse, Guid> warehouses)
    : ICustomerOrderAvailabilityService
{
    public async Task<CustomerOrderAvailabilityDto> AssessAsync(
        CustomerOrder order,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CustomerOrderLineAvailabilityDto>();

        foreach (var line in order.Lines.Where(x => x.IsActive && x.RequiresInventory).OrderBy(x => x.LineNumber))
        {
            var warehouseId = line.WarehouseId!.Value;
            var productVariantId = line.ProductVariantId!.Value;
            var warehouse = await warehouses.GetByIdAsync(warehouseId, cancellationToken);
            var canAllocate = warehouse is { IsActive: true, AllowsSalesAllocation: true };
            var balance = canAllocate
                ? await balances.GetByWarehouseAndVariantAsync(warehouseId, productVariantId, cancellationToken)
                : null;

            var reservationSpec = new Specification<OAS.Domain.Entities.Inventory.StockReservation>().Where(x =>
                x.SourceModule == SalesSourceReferences.Module &&
                x.SourceDocumentType == SalesSourceReferences.CustomerOrder &&
                x.SourceDocumentId == order.Id &&
                x.SourceLineId == line.Id &&
                x.Status == StockReservationStatus.Active &&
                x.IsActive);
            var orderReservations = await reservations.ListAsync(reservationSpec, cancellationToken);
            var alreadyReserved = canAllocate ? orderReservations.Sum(x => x.Quantity) : 0m;

            var onHand = balance?.OnHandQuantity ?? 0m;
            var reserved = balance?.ReservedQuantity ?? 0m;
            var available = balance?.AvailableQuantity ?? 0m;
            var remainingToCover = Math.Max(0m, line.Quantity - alreadyReserved);
            var shortage = Math.Max(0m, remainingToCover - available);
            var status = shortage <= 0m
                ? "Available"
                : alreadyReserved > 0m || available > 0m
                    ? "PartiallyAvailable"
                    : "Unavailable";

            results.Add(new CustomerOrderLineAvailabilityDto(
                line.Id,
                productVariantId,
                warehouseId,
                line.Quantity,
                onHand,
                reserved,
                available,
                alreadyReserved,
                shortage,
                status));
        }

        return new CustomerOrderAvailabilityDto(order.Id, results);
    }
}
