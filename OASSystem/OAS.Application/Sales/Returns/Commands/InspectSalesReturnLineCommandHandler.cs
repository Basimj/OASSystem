using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Inventory.Services;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;
using DomainCondition = OAS.Domain.Sales.Enums.SalesReturnCondition;
using DomainDisposition = OAS.Domain.Sales.Enums.SalesReturnStockDisposition;
using DomainReturnStatus = OAS.Domain.Sales.Enums.SalesReturnStatus;

namespace OAS.Application.Sales.Returns.Commands;

public sealed class InspectSalesReturnLineCommandHandler(
    ISalesReturnAggregateRepository returns,
    ISalesReturnQueryService query,
    IReadRepository<Warehouse, Guid> warehouses,
    IInventoryTransactionRepository transactions,
    IRepository<InventoryTransactionLine, Guid> transactionLines,
    IInventoryPostingService inventoryPosting,
    ISequenceNumberGenerator sequences,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<InspectSalesReturnLineCommand, SalesReturnDto>
{
    private const string ReleaseReferenceType = "SalesReturnInspectionRelease";

    public async Task<SalesReturnDto> Handle(InspectSalesReturnLineCommand request, CancellationToken ct)
    {
        var salesReturn = await returns.GetAggregateAsync(request.ReturnId, true, ct)
            ?? throw new NotFoundException(nameof(SalesReturn), request.ReturnId);
        if (salesReturn.Status != DomainReturnStatus.Posted)
            throw new ConflictException("sales_return_inspection_requires_posted", "لا يمكن فحص/تسريح المخزون إلا بعد ترحيل مرتجع المبيعات.");

        var line = salesReturn.Lines.SingleOrDefault(x => x.Id == request.LineId && x.IsActive)
            ?? throw new NotFoundException(nameof(SalesReturnLine), request.LineId);
        if (!line.RequiresInventory)
            throw new ConflictException("sales_return_inspection_not_inventory", "السطر المحدد لا يمثل مخزونًا قابلًا للفحص.");

        SalesConcurrency.Ensure(request.Request.RowVersion, line.RowVersion, "سطر مرتجع المبيعات");
        if (line.StockDisposition != DomainDisposition.Quarantine)
            throw new ConflictException("sales_return_line_not_quarantined", "يمكن تسجيل الفحص أو التسريح فقط لسطر موجود حاليًا في الحجر.");
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
            throw new ForbiddenException();

        var condition = (DomainCondition)(byte)request.Request.Condition;
        var disposition = (DomainDisposition)(byte)request.Request.Disposition;
        if (condition == DomainCondition.Unknown)
            throw new ConflictException("sales_return_inspection_condition_required", "حدد نتيجة فحص الصنف المرتجع.");
        if (disposition is not (DomainDisposition.Quarantine or DomainDisposition.Restock))
            throw new ConflictException("sales_return_inspection_disposition_invalid", "الفحص يسمح بالإبقاء في الحجر أو التسريح إلى مخزون قابل للبيع فقط. الإتلاف يتم عبر إجراء مخزني مستقل.");

        var now = timeProvider.GetUtcNow();
        if (disposition == DomainDisposition.Quarantine)
        {
            if (request.Request.DestinationWarehouseId.HasValue)
                throw new ConflictException("sales_return_inspection_destination_not_allowed", "لا تحدد مخزن وجهة عند إبقاء الصنف في الحجر.");
            try
            {
                line.RecordInspection(condition, DomainDisposition.Quarantine, now, currentUser.UserId!);
            }
            catch (DomainException ex)
            {
                throw new ConflictException("sales_return_inspection_invalid", ex.Message);
            }
        }
        else
        {
            if (condition != DomainCondition.Resellable)
                throw new ConflictException("sales_return_release_requires_resellable", "لا يمكن تسريح الصنف من الحجر إلا بعد إثبات أنه قابل لإعادة البيع.");
            if (!request.Request.DestinationWarehouseId.HasValue || request.Request.DestinationWarehouseId == Guid.Empty)
                throw new ConflictException("sales_return_release_destination_required", "حدد مخزنًا قابلًا للبيع لتسريح الصنف من الحجر.");

            var sourceWarehouseId = line.WarehouseId!.Value;
            var sourceWarehouse = await warehouses.GetByIdAsync(sourceWarehouseId, ct)
                ?? throw new ConflictException("sales_return_quarantine_warehouse_missing", "مخزن الحجر المرتبط بسطر المرتجع غير موجود.");
            if (!sourceWarehouse.IsActive || sourceWarehouse.AllowsSalesAllocation)
                throw new ConflictException("sales_return_quarantine_warehouse_invalid", "مخزن مصدر التسريح يجب أن يكون مخزن حجر نشطًا وغير متاح للتخصيص البيعي.");

            var destinationWarehouse = await warehouses.GetByIdAsync(request.Request.DestinationWarehouseId.Value, ct)
                ?? throw new ConflictException("sales_return_release_warehouse_missing", "مخزن التسريح المحدد غير موجود.");
            if (!destinationWarehouse.IsActive || !destinationWarehouse.AllowsSalesAllocation)
                throw new ConflictException("sales_return_release_warehouse_not_sellable", "مخزن التسريح يجب أن يكون نشطًا ومسموحًا منه بالتخصيص البيعي.");
            if (destinationWarehouse.Id == sourceWarehouseId)
                throw new ConflictException("sales_return_release_same_warehouse", "مخزن التسريح يجب أن يختلف عن مخزن الحجر.");

            var existing = await transactions.ListAsync(
                new Specification<InventoryTransaction>().Where(x =>
                    x.TransactionType == InventoryTransactionType.Transfer &&
                    x.ReferenceType == ReleaseReferenceType &&
                    x.ReferenceId == line.Id),
                ct);
            if (existing.Count > 0)
                throw new ConflictException("sales_return_release_already_processed", "يوجد تحويل مخزني مسجل مسبقًا لتسريح هذا السطر من الحجر.");

            var sequence = await sequences.NextAsync("InventoryTransaction", ct);
            var transfer = new InventoryTransaction(
                $"TXN-{salesReturn.PostingDate.Year:0000}-{sequence:000000}",
                InventoryTransactionType.Transfer,
                now,
                sourceWarehouseId: sourceWarehouseId,
                destinationWarehouseId: destinationWarehouse.Id,
                referenceType: ReleaseReferenceType,
                referenceId: line.Id,
                reason: $"Release inspected sales return {salesReturn.ReturnCode} / line {line.LineNumber}");
            await transactions.AddAsync(transfer, ct);

            var transferLine = new InventoryTransactionLine(
                transfer.Id,
                line.ProductVariantId!.Value,
                line.Quantity,
                line.UnitCostSnapshot ?? 0m,
                $"Sales return inspection release {salesReturn.ReturnCode} / line {line.LineNumber}");
            await transactionLines.AddAsync(transferLine, ct);

            var (_, outgoingLedger) = await inventoryPosting.PostMovementAsync(
                sourceWarehouseId,
                line.ProductVariantId.Value,
                InventoryMovementType.Out,
                line.Quantity,
                line.UnitCostSnapshot ?? 0m,
                transfer.Id,
                transferLine.Id,
                now,
                currentUser.UserId!,
                ct);
            await inventoryPosting.PostMovementAsync(
                destinationWarehouse.Id,
                line.ProductVariantId.Value,
                InventoryMovementType.In,
                line.Quantity,
                outgoingLedger.UnitCost,
                transfer.Id,
                transferLine.Id,
                now,
                currentUser.UserId!,
                ct);
            transfer.Post(now, currentUser.UserId);

            try
            {
                line.RecordInspection(condition, DomainDisposition.Restock, now, currentUser.UserId!, destinationWarehouse.Id);
            }
            catch (DomainException ex)
            {
                throw new ConflictException("sales_return_release_invalid", ex.Message);
            }
        }

        returns.Update(salesReturn);
        await unitOfWork.SaveChangesAsync(ct);
        return await query.GetByIdAsync(salesReturn.Id, ct)
            ?? throw new NotFoundException(nameof(SalesReturn), salesReturn.Id);
    }
}
