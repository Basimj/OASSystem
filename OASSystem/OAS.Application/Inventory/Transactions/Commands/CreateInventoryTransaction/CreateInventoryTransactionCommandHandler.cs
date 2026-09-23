using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Domain.Entities.Inventory;
using DomainType = OAS.Domain.Enums.Inventory.InventoryTransactionType;

namespace OAS.Application.Inventory.Transactions.Commands.CreateInventoryTransaction;

public sealed class CreateInventoryTransactionCommandHandler(
    IInventoryTransactionRepository transactionRepository,
    IRepository<InventoryTransactionLine, Guid> lineRepository,
    IReadRepository<Warehouse, Guid> warehouseRepository,
    IReadRepository<ProductVariant, Guid> variantRepository,
    IReadRepository<Product, Guid> productRepository,
    ISequenceNumberGenerator sequenceNumberGenerator)
    : IRequestHandler<CreateInventoryTransactionCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateInventoryTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (request.SourceWarehouseId.HasValue)
        {
            var source = await warehouseRepository.GetByIdAsync(request.SourceWarehouseId.Value, cancellationToken);
            if (source is null)
                throw new NotFoundException(nameof(Warehouse), request.SourceWarehouseId.Value);
            EnsureActiveWarehouse(source);
        }

        if (request.DestinationWarehouseId.HasValue)
        {
            var destination = await warehouseRepository.GetByIdAsync(request.DestinationWarehouseId.Value, cancellationToken);
            if (destination is null)
                throw new NotFoundException(nameof(Warehouse), request.DestinationWarehouseId.Value);
            EnsureActiveWarehouse(destination);
        }

        var transactionNumber = request.TransactionNumber;
        if (string.IsNullOrWhiteSpace(transactionNumber))
        {
            var seq = await sequenceNumberGenerator.NextAsync("InventoryTransaction", cancellationToken);
            transactionNumber = $"TXN-{DateTime.UtcNow.Year:0000}-{seq:000000}";
        }

        var transaction = new InventoryTransaction(
            transactionNumber,
            (DomainType)(int)request.TransactionType,
            request.TransactionDate,
            request.SourceWarehouseId,
            request.DestinationWarehouseId,
            request.ReferenceType,
            request.ReferenceId,
            request.Reason,
            request.Notes);

        await transactionRepository.AddAsync(transaction, cancellationToken);

        foreach (var lineReq in request.Lines)
        {
            var variant = await variantRepository.GetByIdAsync(lineReq.ProductVariantId, cancellationToken);
            if (variant is null)
                throw new NotFoundException(nameof(ProductVariant), lineReq.ProductVariantId);
            EnsureVariantActive(variant);

            var product = await productRepository.GetByIdAsync(variant.ProductId, cancellationToken);
            if (product is null)
                throw new NotFoundException(nameof(Product), variant.ProductId);
            EnsureProductEligible(product);

            var line = new InventoryTransactionLine(
                transaction.Id,
                lineReq.ProductVariantId,
                lineReq.Quantity,
                lineReq.UnitCost,
                lineReq.Notes);

            await lineRepository.AddAsync(line, cancellationToken);
        }

        return transaction.Id;
    }

    private static void EnsureActiveWarehouse(Warehouse warehouse)
    {
        if (!warehouse.IsActive)
            throw Validation("warehouse_inactive", "Warehouse must be active for inventory transactions.");
    }

    private static void EnsureVariantActive(ProductVariant variant)
    {
        if (!variant.IsActive)
            throw Validation("product_variant_inactive", "Product variant must be active for inventory transactions.");
    }

    private static void EnsureProductEligible(Product product)
    {
        if (!product.IsActive)
            throw Validation("product_inactive", "Product must be active for inventory transactions.");
        if (!product.IsStockItem)
            throw Validation("product_not_stock_item", "Only stock products can be used in inventory transactions.");
    }

    private static RequestValidationException Validation(string code, string message) =>
        new(new Dictionary<string, string[]> { ["inventory"] = [$"{code}: {message}"] });
}
