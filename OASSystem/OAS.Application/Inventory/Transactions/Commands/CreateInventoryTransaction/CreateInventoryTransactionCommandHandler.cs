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
            var sourceExists = await warehouseRepository.ExistsAsync(request.SourceWarehouseId.Value, cancellationToken);
            if (!sourceExists)
                throw new NotFoundException(nameof(Warehouse), request.SourceWarehouseId.Value);
        }

        if (request.DestinationWarehouseId.HasValue)
        {
            var destExists = await warehouseRepository.ExistsAsync(request.DestinationWarehouseId.Value, cancellationToken);
            if (!destExists)
                throw new NotFoundException(nameof(Warehouse), request.DestinationWarehouseId.Value);
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
            var variantExists = await variantRepository.ExistsAsync(lineReq.ProductVariantId, cancellationToken);
            if (!variantExists)
                throw new NotFoundException(nameof(ProductVariant), lineReq.ProductVariantId);

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
}
