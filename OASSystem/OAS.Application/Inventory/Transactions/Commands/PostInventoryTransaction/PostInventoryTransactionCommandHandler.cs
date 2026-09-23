using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;

namespace OAS.Application.Inventory.Transactions.Commands.PostInventoryTransaction;

public sealed class PostInventoryTransactionCommandHandler(
    IInventoryTransactionRepository transactionRepository,
    IInventoryPostingService postingService,
    IReadRepository<Warehouse, Guid> warehouseRepository,
    IReadRepository<ProductVariant, Guid> variantRepository,
    IReadRepository<Product, Guid> productRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<PostInventoryTransactionCommand, Guid>
{
    public async Task<Guid> Handle(
        PostInventoryTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetForUpdateAsync(
            command.TransactionId,
            cancellationToken);

        if (transaction is null)
            throw new NotFoundException(nameof(InventoryTransaction), command.TransactionId);

        if (transaction.Status == InventoryTransactionStatus.Posted)
            throw new ConflictException("transaction_already_posted", "Transaction has already been posted.");

        var lines = await transactionRepository.GetLinesAsync(command.TransactionId, cancellationToken);
        if (lines.Count == 0)
            throw new ConflictException("transaction_has_no_lines", "Cannot post a transaction without lines.");

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var userId = currentUser.UserId ?? "system";

        foreach (var line in lines)
        {
            var variant = await variantRepository.GetByIdAsync(line.ProductVariantId, cancellationToken)
                ?? throw new NotFoundException(nameof(ProductVariant), line.ProductVariantId);
            if (!variant.IsActive)
                throw Validation("product_variant_inactive", "Product variant must be active before posting inventory.");

            var product = await productRepository.GetByIdAsync(variant.ProductId, cancellationToken)
                ?? throw new NotFoundException(nameof(Product), variant.ProductId);
            if (!product.IsActive)
                throw Validation("product_inactive", "Product must be active before posting inventory.");
            if (!product.IsStockItem)
                throw Validation("product_not_stock_item", "Only stock products can be posted to inventory.");

            switch (transaction.TransactionType)
            {
                case InventoryTransactionType.Receipt:
                case InventoryTransactionType.Opening:
                case InventoryTransactionType.SalesReturn:
                case InventoryTransactionType.AdjustmentIncrease:
                {
                    var warehouseId = transaction.DestinationWarehouseId ?? transaction.SourceWarehouseId;
                    if (!warehouseId.HasValue)
                        throw new ConflictException("destination_warehouse_required", "Destination warehouse is required for inbound transactions.");
                    await EnsureWarehouseActiveAsync(warehouseId.Value, cancellationToken);

                    await postingService.PostMovementAsync(
                        warehouseId.Value,
                        line.ProductVariantId,
                        InventoryMovementType.In,
                        line.Quantity,
                        line.UnitCost,
                        transaction.Id,
                        line.Id,
                        transaction.TransactionDate,
                        userId,
                        cancellationToken);
                    break;
                }

                case InventoryTransactionType.Issue:
                case InventoryTransactionType.AdjustmentDecrease:
                case InventoryTransactionType.PurchaseReturn:
                case InventoryTransactionType.ProductionIssue:
                case InventoryTransactionType.Scrap:
                {
                    var warehouseId = transaction.SourceWarehouseId ?? transaction.DestinationWarehouseId;
                    if (!warehouseId.HasValue)
                        throw new ConflictException("source_warehouse_required", "Source warehouse is required for outbound transactions.");
                    await EnsureWarehouseActiveAsync(warehouseId.Value, cancellationToken);

                    await postingService.PostMovementAsync(
                        warehouseId.Value,
                        line.ProductVariantId,
                        InventoryMovementType.Out,
                        line.Quantity,
                        line.UnitCost,
                        transaction.Id,
                        line.Id,
                        transaction.TransactionDate,
                        userId,
                        cancellationToken);
                    break;
                }

                case InventoryTransactionType.Transfer:
                {
                    if (!transaction.SourceWarehouseId.HasValue)
                        throw new ConflictException("source_warehouse_required", "Source warehouse is required for transfer.");
                    if (!transaction.DestinationWarehouseId.HasValue)
                        throw new ConflictException("destination_warehouse_required", "Destination warehouse is required for transfer.");
                    await EnsureWarehouseActiveAsync(transaction.SourceWarehouseId.Value, cancellationToken);
                    await EnsureWarehouseActiveAsync(transaction.DestinationWarehouseId.Value, cancellationToken);

                    var (sourceBal, _) = await postingService.PostMovementAsync(
                        transaction.SourceWarehouseId.Value,
                        line.ProductVariantId,
                        InventoryMovementType.Out,
                        line.Quantity,
                        line.UnitCost,
                        transaction.Id,
                        line.Id,
                        transaction.TransactionDate,
                        userId,
                        cancellationToken);

                    var transferCost = sourceBal.AverageUnitCost > 0 ? sourceBal.AverageUnitCost : line.UnitCost;

                    await postingService.PostMovementAsync(
                        transaction.DestinationWarehouseId.Value,
                        line.ProductVariantId,
                        InventoryMovementType.In,
                        line.Quantity,
                        transferCost,
                        transaction.Id,
                        line.Id,
                        transaction.TransactionDate,
                        userId,
                        cancellationToken);
                    break;
                }

                default:
                    throw new InvalidOperationException($"Unsupported transaction type '{transaction.TransactionType}'.");
            }
        }

        transaction.Post(nowUtc, userId);
        transactionRepository.Update(transaction);

        return transaction.Id;
    }

    private async Task EnsureWarehouseActiveAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseRepository.GetByIdAsync(warehouseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), warehouseId);
        if (!warehouse.IsActive)
            throw Validation("warehouse_inactive", "Warehouse must be active before posting inventory.");
    }

    private static RequestValidationException Validation(string code, string message) =>
        new(new Dictionary<string, string[]> { ["inventory"] = [$"{code}: {message}"] });
}
