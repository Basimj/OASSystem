using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Services;

public sealed class PurchasingInventoryPort(
    OasDbContext dbContext,
    IInventoryBalanceRepository balanceRepository,
    IInventoryTransactionRepository transactionRepository,
    IRepository<InventoryTransactionLine, Guid> transactionLineRepository,
    IInventoryPostingService postingService,
    ISequenceNumberGenerator sequenceNumberGenerator,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IPurchasingInventoryPort
{
    public async Task IncreaseOnOrderAsync(Guid warehouseId, IReadOnlyList<PurchasingOnOrderLine> lines, Guid purchaseOrderId, DateOnly effectiveDate, CancellationToken cancellationToken = default)
    {
        foreach (var line in Consolidate(lines))
        {
            var balance = await balanceRepository.GetByWarehouseAndVariantAsync(warehouseId, line.ProductVariantId, cancellationToken);
            if (balance is null)
            {
                balance = new InventoryBalance(warehouseId, line.ProductVariantId);
                await balanceRepository.AddAsync(balance, cancellationToken);
            }
            balance.IncreaseOnOrder(line.BaseQuantity);
            balanceRepository.Update(balance);
        }
    }

    public async Task DecreaseOnOrderAsync(Guid warehouseId, IReadOnlyList<PurchasingOnOrderLine> lines, Guid purchaseOrderId, DateOnly effectiveDate, CancellationToken cancellationToken = default)
    {
        foreach (var line in Consolidate(lines))
        {
            var balance = await balanceRepository.GetByWarehouseAndVariantAsync(warehouseId, line.ProductVariantId, cancellationToken)
                ?? throw new ConflictException("purchasing_on_order_balance_missing", "تعذر تحرير الكمية تحت الطلب لأن رصيد المخزون المرتبط غير موجود.");
            try { balance.DecreaseOnOrder(line.BaseQuantity); }
            catch (OAS.Domain.Exceptions.DomainException)
            {
                throw new ConflictException("purchasing_on_order_conflict", "تعذر تعديل الكمية تحت الطلب بسبب تعارض في رصيد المخزون. أعد تحميل البيانات وحاول مرة أخرى.");
            }
            balanceRepository.Update(balance);
        }
    }

    public async Task ValidatePostingDateAsync(Guid warehouseId, DateOnly postingDate, CancellationToken cancellationToken = default)
    {
        var periods = await dbContext.Set<FiscalPeriod>().AsNoTracking()
            .Where(x => x.StartDate <= postingDate && x.EndDate >= postingDate).Take(2).ToListAsync(cancellationToken);
        if (periods.Count == 0) throw new ConflictException("fiscal_period_not_found", "لا توجد فترة مالية تغطي تاريخ الترحيل.");
        if (periods.Count > 1) throw new ConflictException("fiscal_period_overlap", "يوجد أكثر من فترة مالية تغطي تاريخ الترحيل.");
        var period = periods[0];
        if (period.Status == FiscalPeriodStatus.Closed || period.InventoryLocked)
            throw new ConflictException("inventory_period_locked", "المخزون مغلق لهذا التاريخ.");
    }

    public async Task<PurchasingInventoryPostingResult> PostPurchaseReceiptAsync(PurchasingReceiptInventoryContext context, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Set<InventoryTransaction>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReferenceType == "PurchaseReceipt" && x.ReferenceId == context.PurchaseReceiptId, cancellationToken);
        if (existing is not null) return new PurchasingInventoryPostingResult(existing.Id);

        var sequence = await sequenceNumberGenerator.NextAsync("InventoryTransaction", cancellationToken);
        var movementAt = new DateTimeOffset(context.PostingDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var transaction = new InventoryTransaction($"TXN-{context.PostingDate.Year:0000}-{sequence:000000}",
            InventoryTransactionType.Receipt, movementAt, null, context.WarehouseId, "PurchaseReceipt", context.PurchaseReceiptId,
            "Purchase receipt", context.ReceiptCode);
        await transactionRepository.AddAsync(transaction, cancellationToken);

        var userId = currentUser.UserId ?? "system";
        foreach (var source in context.Lines.Where(x => x.BaseQuantity > 0))
        {
            var line = new InventoryTransactionLine(transaction.Id, source.ProductVariantId, source.BaseQuantity, source.UnitCost,
                string.IsNullOrWhiteSpace(source.BatchCode) ? null : $"Batch: {source.BatchCode}");
            await transactionLineRepository.AddAsync(line, cancellationToken);
            await postingService.PostMovementAsync(context.WarehouseId, source.ProductVariantId, InventoryMovementType.In,
                source.BaseQuantity, source.UnitCost, transaction.Id, line.Id, movementAt, userId, cancellationToken);
        }
        transaction.Post(timeProvider.GetUtcNow(), userId);
        transactionRepository.Update(transaction);
        return new PurchasingInventoryPostingResult(transaction.Id);
    }

    private static IReadOnlyList<PurchasingOnOrderLine> Consolidate(IReadOnlyList<PurchasingOnOrderLine> lines) =>
        lines.Where(x => x.BaseQuantity > 0).GroupBy(x => x.ProductVariantId)
            .Select(g => new PurchasingOnOrderLine(g.Key, g.Sum(x => x.BaseQuantity))).ToArray();
}
