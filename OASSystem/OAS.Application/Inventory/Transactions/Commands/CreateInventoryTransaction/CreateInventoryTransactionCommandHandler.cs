using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Transactions;
using OAS.Domain.Entities.Inventory;
using DomainType = OAS.Domain.Enums.Inventory.InventoryTransactionType;

namespace OAS.Application.Inventory.Transactions.Commands.CreateInventoryTransaction;

public sealed class CreateInventoryTransactionCommandHandler(
    IInventoryTransactionRepository transactionRepository,
    IRepository<InventoryTransactionLine, Guid> lineRepository,
    IReadRepository<Warehouse, Guid> warehouseRepository,
    IReadRepository<ProductVariant, Guid> variantRepository,
    IReadRepository<Product, Guid> productRepository,
    ISequenceNumberGenerator sequenceNumberGenerator,
    IInventoryBalanceRepository balanceRepository)
    : IRequestHandler<CreateInventoryTransactionCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateInventoryTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        // Never trust the client-side transaction type list: enforce this at the API handler.
        if (!ManualInventoryTransactionPolicy.IsAllowed((DomainType)(int)request.TransactionType))
            throw new ConflictException(ManualInventoryTransactionPolicy.ErrorCode, ManualInventoryTransactionPolicy.ErrorMessage);

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

        if (request.SourceWarehouseId is not { } sourceId || sourceId == Guid.Empty)
            throw new ConflictException("source_warehouse_required", "يجب تحديد المخزن المصدر.");

        // Validate every line before allocating a number or adding any entities.
        // A draft is not a reservation; stock must be checked again at posting.
        var validatedLines = new List<(OAS.Contracts.Inventory.Transactions.CreateInventoryTransactionLineRequest Request, decimal UnitCost)>();
        foreach (var lineReq in request.Lines)
        {
            var variant = await variantRepository.GetByIdAsync(lineReq.ProductVariantId, cancellationToken)
                ?? throw new NotFoundException(nameof(ProductVariant), lineReq.ProductVariantId);
            EnsureVariantActive(variant);
            var product = await productRepository.GetByIdAsync(variant.ProductId, cancellationToken)
                ?? throw new NotFoundException(nameof(Product), variant.ProductId);
            EnsureProductEligible(product);

            var balance = await balanceRepository.GetByWarehouseAndVariantAsync(sourceId, variant.Id, cancellationToken);
            var available = balance?.AvailableQuantity ?? 0m;
            if (balance is null || available < lineReq.Quantity || balance.OnHandQuantity < lineReq.Quantity)
                throw new ConflictException("insufficient_stock",
                    $"الصنف {variant.SKU}: الكمية المتاحة في المخزن المصدر {available:0.###}، والمطلوبة {lineReq.Quantity:0.###}.");

            var unitCost = balance.AverageUnitCost;
            if (unitCost > 0m && lineReq.Quantity > 9999999999999999.99m / unitCost)
                throw new ConflictException("line_total_cost_precision_invalid", "إجمالي تكلفة البند أكبر من الحد المسموح به.");
            validatedLines.Add((lineReq, unitCost));
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
        foreach (var (lineReq, unitCost) in validatedLines)
        {
            var line = new InventoryTransactionLine(
                transaction.Id, lineReq.ProductVariantId, lineReq.Quantity, unitCost, lineReq.Notes);
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
