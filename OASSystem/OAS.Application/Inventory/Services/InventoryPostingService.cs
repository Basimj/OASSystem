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

        var balance = await balanceRepository.GetByWarehouseAndVariantAsync(
            warehouseId,
            productVariantId,
            cancellationToken);

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
            quantityOut = 0;
            appliedUnitCost = unitCost;
        }
        else if (movementType == InventoryMovementType.Out)
        {
            if (balance is null || balance.OnHandQuantity < quantity)
            {
                var available = balance?.OnHandQuantity ?? 0;
                throw new ConflictException(
                    "insufficient_stock",
                    $"Insufficient stock for variant '{productVariantId}' in warehouse '{warehouseId}'. Available: {available}, Required: {quantity}");
            }

            // Zero is a valid weighted-average cost (for example a free opening balance).
            appliedUnitCost = balance.AverageUnitCost;
            balance.ApplyOutbound(quantity, movementDate);
            balanceRepository.Update(balance);

            quantityIn = 0;
            quantityOut = quantity;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(movementType), $"Unsupported movement type '{movementType}'.");
        }

        var sequenceNumber = await sequenceNumberGenerator.NextAsync("InventoryLedger", cancellationToken);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var ledger = new InventoryLedger(
            sequenceNumber,
            transactionId,
            transactionLineId,
            warehouseId,
            productVariantId,
            movementType,
            quantityIn,
            quantityOut,
            balance.OnHandQuantity,
            appliedUnitCost,
            balance.AverageUnitCost,
            balance.InventoryValue,
            movementDate,
            nowUtc,
            createdBy);

        await ledgerRepository.AddAsync(ledger, cancellationToken);

        return (balance, ledger);
    }
}
