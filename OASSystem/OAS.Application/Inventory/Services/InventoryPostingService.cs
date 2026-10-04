using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.Services;

public sealed class InventoryPostingService(
    IInventoryBalanceRepository balanceRepository,
    IRepository<InventoryLedger, Guid> ledgerRepository,
    ISequenceNumberGenerator sequenceNumberGenerator,
    TimeProvider timeProvider)
    : IInventoryPostingService
{
    public async Task<(InventoryBalance Balance, InventoryLedger Ledger)> PostMovementAsync(
        Guid warehouseId,
        Guid productVariantId,
        InventoryMovementType movementType,
        decimal quantity,
        decimal unitCost,
        Guid transactionId,
        Guid transactionLineId,
        DateTimeOffset movementDate,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (unitCost < 0)
            throw new ArgumentOutOfRangeException(nameof(unitCost), "Unit cost cannot be negative.");

        var balance = await balanceRepository.GetByWarehouseAndVariantAsync(warehouseId, productVariantId, cancellationToken);
        decimal quantityIn = 0;
        decimal quantityOut = 0;
        decimal appliedUnitCost = unitCost;

        if (movementType == InventoryMovementType.In)
        {
            if (balance is null)
            {
                balance = new InventoryBalance(warehouseId, productVariantId, 0, 0, 0, 0, 0);
                await balanceRepository.AddAsync(balance, cancellationToken);
            }

            balance.ApplyInbound(quantity, unitCost, movementDate);
            balanceRepository.Update(balance);
            quantityIn = quantity;
        }
        else if (movementType == InventoryMovementType.Out)
        {
            if (balance is null || balance.AvailableQuantity < quantity)
            {
                var available = balance?.AvailableQuantity ?? 0;
                throw new ConflictException("insufficient_stock", $"Insufficient available stock. Available: {available}, Required: {quantity}");
            }

            appliedUnitCost = balance.AverageUnitCost;
            balance.ApplyOutbound(quantity, movementDate);
            balanceRepository.Update(balance);
            quantityOut = quantity;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(movementType), $"Unsupported movement type '{movementType}'.");
        }

        var ledger = await CreateLedgerAsync(balance, transactionId, transactionLineId, warehouseId, productVariantId,
            movementType, quantityIn, quantityOut, appliedUnitCost, movementDate, createdBy, cancellationToken);
        return (balance, ledger);
    }

    public async Task<decimal> GetAvailableOutboundUnitCostAsync(
        Guid warehouseId,
        Guid productVariantId,
        decimal quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        var balance = await balanceRepository.GetByWarehouseAndVariantAsync(warehouseId, productVariantId, cancellationToken);
        if (balance is null || balance.AvailableQuantity < quantity || balance.OnHandQuantity < quantity)
            throw new ConflictException("insufficient_stock", $"Insufficient available stock. Available: {balance?.AvailableQuantity ?? 0m}, Required: {quantity}");
        return balance.AverageUnitCost;
    }

    public async Task<decimal> GetOutboundUnitCostAsync(
        Guid warehouseId,
        Guid productVariantId,
        decimal quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        var balance = await balanceRepository.GetByWarehouseAndVariantAsync(warehouseId, productVariantId, cancellationToken);
        if (balance is null || balance.ReservedQuantity < quantity || balance.OnHandQuantity < quantity)
            throw new ConflictException("sales_reservation_conflict", "Reserved or on-hand stock is no longer sufficient for the sale.");
        return balance.AverageUnitCost;
    }

    public async Task<(InventoryBalance Balance, InventoryLedger Ledger)> PostReservedOutboundAsync(
        Guid warehouseId,
        Guid productVariantId,
        decimal quantity,
        Guid transactionId,
        Guid transactionLineId,
        DateTimeOffset movementDate,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        var balance = await balanceRepository.GetByWarehouseAndVariantAsync(warehouseId, productVariantId, cancellationToken);
        if (balance is null)
            throw new ConflictException("sales_reservation_conflict", "Inventory balance no longer exists for the reserved sale.");

        var appliedUnitCost = balance.AverageUnitCost;
        balance.ConsumeReservation(quantity, movementDate);
        balanceRepository.Update(balance);

        var ledger = await CreateLedgerAsync(balance, transactionId, transactionLineId, warehouseId, productVariantId,
            InventoryMovementType.Out, 0m, quantity, appliedUnitCost, movementDate, createdBy, cancellationToken);
        return (balance, ledger);
    }

    private async Task<InventoryLedger> CreateLedgerAsync(
        InventoryBalance balance,
        Guid transactionId,
        Guid transactionLineId,
        Guid warehouseId,
        Guid productVariantId,
        InventoryMovementType movementType,
        decimal quantityIn,
        decimal quantityOut,
        decimal unitCost,
        DateTimeOffset movementDate,
        string createdBy,
        CancellationToken cancellationToken)
    {
        var sequenceNumber = await sequenceNumberGenerator.NextAsync("InventoryLedger", cancellationToken);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var ledger = new InventoryLedger(
            sequenceNumber, transactionId, transactionLineId, warehouseId, productVariantId, movementType,
            quantityIn, quantityOut, balance.OnHandQuantity, unitCost, balance.AverageUnitCost,
            balance.InventoryValue, movementDate, nowUtc, createdBy);
        await ledgerRepository.AddAsync(ledger, cancellationToken);
        return ledger;
    }
}
