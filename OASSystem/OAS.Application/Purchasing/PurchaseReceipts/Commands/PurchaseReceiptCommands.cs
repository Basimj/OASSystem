using MediatR;
using Microsoft.Extensions.Logging;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Common;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Domain.Purchasing.Entities;
using DomainOrderStatus = OAS.Domain.Purchasing.Enums.PurchaseOrderStatus;
using DomainReceiptStatus = OAS.Domain.Purchasing.Enums.PurchaseReceiptStatus;

namespace OAS.Application.Purchasing.PurchaseReceipts.Commands;

public sealed record CreatePurchaseReceiptCommand(CreatePurchaseReceiptRequest Request) : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Receipts.Create]; }
public sealed record UpdatePurchaseReceiptCommand(Guid Id, UpdatePurchaseReceiptRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Receipts.Edit]; }
public sealed record ConfirmPurchaseReceiptCommand(Guid Id, ConfirmPurchaseReceiptRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Receipts.Confirm]; }
public sealed record PostPurchaseReceiptCommand(Guid Id, PostPurchaseReceiptRequest Request) : INonTransactionalCommand<PurchaseReceiptPostResultDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Receipts.Post]; }
public sealed record CancelPurchaseReceiptCommand(Guid Id, CancelPurchaseReceiptRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Receipts.Cancel]; }

public sealed class CreatePurchaseReceiptCommandHandler(
    IPurchaseReceiptRepository receiptRepository,
    IPurchaseOrderRepository orderRepository,
    IPurchasingReferenceDataPort references,
    IPurchasingCodeService codes) : IRequestHandler<CreatePurchaseReceiptCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseReceiptCommand command, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(command.Request.PurchaseOrderId, ct) ?? throw new NotFoundException("PurchaseOrder", command.Request.PurchaseOrderId);
        if (order.Status is not (DomainOrderStatus.Sent or DomainOrderStatus.PartiallyReceived))
            throw new ConflictException("purchasing_order_not_receivable", "أمر الشراء غير مرسل للمورد أو غير متاح للاستلام.");
        PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(order.SupplierId, ct));
        PurchasingApplicationGuard.Warehouse(await references.GetWarehouseAsync(order.DestinationWarehouseId, ct));

        var id = Guid.NewGuid();
        var receipt = PurchaseReceipt.Create(id, await codes.NextPurchaseReceiptCodeAsync(command.Request.ReceiptDate, ct), order.Id,
            order.SupplierId, order.DestinationWarehouseId, command.Request.ReceiptDate, command.Request.PostingDate,
            command.Request.SupplierDeliveryCode, command.Request.Notes);
        await PopulateLinesAsync(receipt, order, command.Request.Lines.Select(x => new ReceiptLineInput(x.PurchaseOrderLineId,x.LineSequence,x.ReceivedQuantity,x.AcceptedQuantity,x.RejectedQuantity,x.ActualUnitCost,x.ExpiryDate,x.BatchCode,x.Notes,null,null)).ToArray(), receiptRepository, ct);
        await receiptRepository.AddAsync(receipt, ct);
        return id;
    }

    internal static async Task PopulateLinesAsync(PurchaseReceipt receipt, PurchaseOrder order, IReadOnlyList<ReceiptLineInput> inputs,
        IPurchaseReceiptRepository receiptRepository, CancellationToken ct)
    {
        var orderLines = order.Lines.ToDictionary(x => x.Id);
        foreach (var input in inputs.OrderBy(x=>x.LineSequence))
        {
            if (!orderLines.TryGetValue(input.PurchaseOrderLineId, out var poLine))
                throw new ConflictException("purchasing_receipt_line_not_in_po", "لا يسمح بإضافة منتج أو سطر غير موجود في أمر الشراء.");
            var previous = await receiptRepository.GetPostedAcceptedQuantityAsync(poLine.Id, ct);
            if (input.AcceptedQuantity > poLine.OrderedQuantity - previous)
                throw new ConflictException("purchasing_receipt_over_receipt", "لا يمكن استلام كمية أكبر من الكمية المتبقية في أمر الشراء.");
            receipt.AddLine(PurchaseReceiptLine.Create(input.Id ?? Guid.NewGuid(), receipt.Id, poLine.Id, input.LineSequence,
                poLine.ProductVariantId, poLine.OrderedQuantity, previous, input.ReceivedQuantity, input.AcceptedQuantity,
                input.RejectedQuantity, poLine.UnitConversionFactor, input.ActualUnitCost, input.ExpiryDate, input.BatchCode, input.Notes));
        }
    }

    internal sealed record ReceiptLineInput(Guid PurchaseOrderLineId,int LineSequence,decimal ReceivedQuantity,decimal AcceptedQuantity,
        decimal RejectedQuantity,decimal ActualUnitCost,DateOnly? ExpiryDate,string? BatchCode,string? Notes,Guid? Id,string? RowVersion);
}

public sealed class UpdatePurchaseReceiptCommandHandler(
    IPurchaseReceiptRepository receiptRepository,
    IPurchaseOrderRepository orderRepository) : IRequestHandler<UpdatePurchaseReceiptCommand>
{
    public async Task Handle(UpdatePurchaseReceiptCommand command, CancellationToken ct)
    {
        var receipt = await receiptRepository.GetForUpdateAsync(command.Id, ct) ?? throw new NotFoundException("PurchaseReceipt", command.Id);
        PurchasingRowVersion.EnsureMatches(receipt.RowVersion, command.Request.RowVersion, "Purchase receipt");
        if (receipt.Status != DomainReceiptStatus.Draft) throw new ConflictException("purchasing_receipt_not_draft", "يمكن تعديل سند الاستلام عندما يكون مسودة فقط.");
        var order = await orderRepository.GetByIdAsync(receipt.PurchaseOrderId, ct) ?? throw new NotFoundException("PurchaseOrder", receipt.PurchaseOrderId);
        receipt.UpdateHeader(command.Request.ReceiptDate, command.Request.PostingDate, command.Request.SupplierDeliveryCode, command.Request.Notes);

        var incomingIds = command.Request.Lines.Where(x=>x.Id.HasValue).Select(x=>x.Id!.Value).ToHashSet();
        foreach(var old in receipt.Lines.Where(x=>!incomingIds.Contains(x.Id)).ToArray()) receipt.RemoveLine(old.Id);
        var orderLines=order.Lines.ToDictionary(x=>x.Id);
        foreach(var input in command.Request.Lines.OrderBy(x=>x.LineSequence))
        {
            if(!orderLines.TryGetValue(input.PurchaseOrderLineId,out var poLine)) throw new ConflictException("purchasing_receipt_line_not_in_po","لا يسمح بإضافة سطر غير موجود في أمر الشراء.");
            var previous=await receiptRepository.GetPostedAcceptedQuantityAsync(poLine.Id,ct);
            if(input.AcceptedQuantity>poLine.OrderedQuantity-previous) throw new ConflictException("purchasing_receipt_over_receipt","لا يمكن استلام كمية أكبر من الكمية المتبقية في أمر الشراء.");
            if(input.Id.HasValue)
            {
                var existing=receipt.Lines.SingleOrDefault(x=>x.Id==input.Id.Value)??throw new ConflictException("purchasing_receipt_line_missing","أحد بنود الاستلام لم يعد موجودًا.");
                if(string.IsNullOrWhiteSpace(input.RowVersion))throw new ConcurrencyException("Purchase receipt line row version is required.");
                PurchasingRowVersion.EnsureMatches(existing.RowVersion,input.RowVersion,"Purchase receipt line");
                if(existing.PurchaseOrderLineId!=input.PurchaseOrderLineId || existing.ProductVariantId!=poLine.ProductVariantId)
                {
                    receipt.RemoveLine(existing.Id);
                    receipt.AddLine(PurchaseReceiptLine.Create(existing.Id,receipt.Id,poLine.Id,input.LineSequence,poLine.ProductVariantId,poLine.OrderedQuantity,previous,input.ReceivedQuantity,input.AcceptedQuantity,input.RejectedQuantity,poLine.UnitConversionFactor,input.ActualUnitCost,input.ExpiryDate,input.BatchCode,input.Notes));
                }
                else
                {
                    existing.Update(input.ReceivedQuantity,input.AcceptedQuantity,input.RejectedQuantity,poLine.UnitConversionFactor,input.ActualUnitCost,input.ExpiryDate,input.BatchCode,input.Notes);
                }
            }
            else receipt.AddLine(PurchaseReceiptLine.Create(Guid.NewGuid(),receipt.Id,poLine.Id,input.LineSequence,poLine.ProductVariantId,poLine.OrderedQuantity,previous,input.ReceivedQuantity,input.AcceptedQuantity,input.RejectedQuantity,poLine.UnitConversionFactor,input.ActualUnitCost,input.ExpiryDate,input.BatchCode,input.Notes));
        }
        await receiptRepository.ReplaceLinesAsync(receipt,ct);
        receiptRepository.Update(receipt);
    }
}

public sealed class ConfirmPurchaseReceiptCommandHandler(IPurchaseReceiptRepository repository, ICurrentUser currentUser, TimeProvider timeProvider) : IRequestHandler<ConfirmPurchaseReceiptCommand>
{
    public async Task Handle(ConfirmPurchaseReceiptCommand c,CancellationToken ct){var e=await repository.GetForUpdateAsync(c.Id,ct)??throw new NotFoundException("PurchaseReceipt",c.Id);PurchasingRowVersion.EnsureMatches(e.RowVersion,c.Request.RowVersion,"Purchase receipt");e.Confirm(timeProvider.GetUtcNow(),currentUser.UserId);repository.Update(e);}
}

public sealed class CancelPurchaseReceiptCommandHandler(IPurchaseReceiptRepository repository, ICurrentUser currentUser, TimeProvider timeProvider) : IRequestHandler<CancelPurchaseReceiptCommand>
{
    public async Task Handle(CancelPurchaseReceiptCommand c,CancellationToken ct){var e=await repository.GetForUpdateAsync(c.Id,ct)??throw new NotFoundException("PurchaseReceipt",c.Id);PurchasingRowVersion.EnsureMatches(e.RowVersion,c.Request.RowVersion,"Purchase receipt");e.Cancel(timeProvider.GetUtcNow(),currentUser.UserId,c.Request.Reason);repository.Update(e);}
}

public sealed class PostPurchaseReceiptCommandHandler(
    IPurchaseReceiptRepository receiptRepository,
    IPurchaseOrderRepository orderRepository,
    IPurchasingReferenceDataPort references,
    IPurchasingInventoryPort inventory,
    IPurchasingAccountingPort accounting,
    IPurchaseRequestRepository requestRepository,
    ICustomerOrderFulfillmentService fulfillment,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    ILogger<PostPurchaseReceiptCommandHandler> logger) : IRequestHandler<PostPurchaseReceiptCommand, PurchaseReceiptPostResultDto>
{
    public async Task<PurchaseReceiptPostResultDto> Handle(PostPurchaseReceiptCommand command, CancellationToken ct)
    {
        // Purchase receipt posting is its own committed transaction. Customer-order fulfillment
        // runs only after that commit, so a sales-side validation failure can never roll back
        // inventory/accounting effects of a valid purchasing receipt.
        var posted = await unitOfWork.ExecuteInTransactionAsync(
            tx => PostReceiptAsync(command, tx),
            ct);

        if (posted.CustomerOrderIds.Count > 0)
        {
            try
            {
                await unitOfWork.ExecuteInTransactionAsync(async tx =>
                {
                    await fulfillment.ReconcileOrdersAsync(posted.CustomerOrderIds, tx);
                    return true;
                }, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The receipt is already committed. Keep purchasing successful and leave the
                // customer order resumable through Checkout/reconciliation instead of returning
                // an error that misleadingly suggests the receipt was rolled back.
                logger.LogWarning(
                    ex,
                    "Purchase receipt {ReceiptId} posted successfully, but follow-up sales fulfillment failed for customer orders {CustomerOrderIds}.",
                    posted.Result.PurchaseReceiptId,
                    string.Join(",", posted.CustomerOrderIds));
            }
        }

        return posted.Result;
    }

    private async Task<PostedReceiptWork> PostReceiptAsync(PostPurchaseReceiptCommand command, CancellationToken ct)
    {
        var receipt = await receiptRepository.GetForUpdateAsync(command.Id, ct)
            ?? throw new NotFoundException("PurchaseReceipt", command.Id);

        if (receipt.Status == DomainReceiptStatus.Posted && receipt.InventoryTransactionId.HasValue && receipt.JournalEntryId.HasValue)
        {
            var linked = await ResolveCustomerOrderIdsAsync(receipt, ct);
            return new PostedReceiptWork(
                new PurchaseReceiptPostResultDto(receipt.Id, receipt.InventoryTransactionId.Value, receipt.JournalEntryId.Value, receipt.PurchaseOrderId, receipt.ReceiptCode),
                linked);
        }

        PurchasingRowVersion.EnsureMatches(receipt.RowVersion, command.Request.RowVersion, "Purchase receipt");
        if (receipt.Status != DomainReceiptStatus.Confirmed)
            throw new ConflictException("purchasing_receipt_not_confirmed", "يجب تأكيد سند الاستلام قبل الترحيل.");

        var order = await orderRepository.GetForUpdateAsync(receipt.PurchaseOrderId, ct)
            ?? throw new NotFoundException("PurchaseOrder", receipt.PurchaseOrderId);
        if (order.Status is not (DomainOrderStatus.Sent or DomainOrderStatus.PartiallyReceived))
            throw new ConflictException("purchasing_order_not_receivable", "أمر الشراء غير متاح للاستلام.");

        PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(receipt.SupplierId, ct));
        PurchasingApplicationGuard.Warehouse(await references.GetWarehouseAsync(receipt.WarehouseId, ct));
        await inventory.ValidatePostingDateAsync(receipt.WarehouseId, receipt.PostingDate, ct);
        await accounting.ValidatePostingPeriodAsync(receipt.PostingDate, ct);

        var orderLines = order.Lines.ToDictionary(x => x.Id);
        var inventoryLines = new List<PurchasingReceiptInventoryLine>();
        var journalLines = new List<PurchasingReceiptJournalLine>();
        var decreaseLines = new List<PurchasingOnOrderLine>();
        var receivedBaseAfter = new Dictionary<Guid, decimal>();
        foreach (var poLine in order.Lines)
        {
            var previous = await receiptRepository.GetPostedAcceptedQuantityAsync(poLine.Id, ct);
            receivedBaseAfter[poLine.Id] = Math.Round(previous * poLine.UnitConversionFactor, 3);
        }

        foreach (var line in receipt.Lines)
        {
            if (!orderLines.TryGetValue(line.PurchaseOrderLineId, out var poLine))
                throw new ConflictException("purchasing_receipt_line_not_in_po", "سطر الاستلام لم يعد مرتبطًا بسطر صالح في أمر الشراء.");
            var previousBase = receivedBaseAfter[poLine.Id];
            var previous = poLine.UnitConversionFactor == 0 ? 0 : previousBase / poLine.UnitConversionFactor;
            if (previous + line.AcceptedQuantity > poLine.OrderedQuantity)
                throw new ConflictException("purchasing_receipt_over_receipt", "لا يمكن استلام كمية أكبر من الكمية المتبقية في أمر الشراء.");
            receivedBaseAfter[poLine.Id] = previousBase + line.BaseAcceptedQuantity;
            if (line.AcceptedQuantity <= 0) continue;
            inventoryLines.Add(new(line.Id, line.ProductVariantId, line.BaseAcceptedQuantity, line.ActualUnitCost, line.ExpiryDate, line.BatchCode));
            journalLines.Add(new(line.Id, line.ProductVariantId, line.TotalAcceptedCost));
            decreaseLines.Add(new(line.ProductVariantId, line.BaseAcceptedQuantity));
        }

        if (inventoryLines.Count == 0)
            throw new ConflictException("purchasing_receipt_no_accepted_quantity", "لا توجد كمية مقبولة لترحيلها إلى المخزون.");

        var inventoryResult = await inventory.PostPurchaseReceiptAsync(
            new(receipt.Id, receipt.ReceiptCode, receipt.WarehouseId, receipt.PostingDate, inventoryLines), ct);
        await inventory.DecreaseOnOrderAsync(receipt.WarehouseId, decreaseLines, order.Id, receipt.PostingDate, ct);
        var accountingResult = await accounting.PostPurchaseReceiptJournalAsync(
            new(receipt.Id, receipt.ReceiptCode, receipt.SupplierId, receipt.PostingDate, journalLines), ct);

        receipt.MarkPosted(inventoryResult.InventoryTransactionId, accountingResult.JournalEntryId, timeProvider.GetUtcNow(), currentUser.UserId);
        receiptRepository.Update(receipt);
        var fullyReceived = order.Lines.All(x => receivedBaseAfter.TryGetValue(x.Id, out var qty) && qty >= x.BaseQuantity);
        order.MarkReceived(fullyReceived);
        orderRepository.Update(order);

        var customerOrderIds = await ResolveCustomerOrderIdsAsync(receipt, ct);
        return new PostedReceiptWork(
            new PurchaseReceiptPostResultDto(receipt.Id, inventoryResult.InventoryTransactionId, accountingResult.JournalEntryId, order.Id, receipt.ReceiptCode),
            customerOrderIds);
    }

    private async Task<IReadOnlyList<Guid>> ResolveCustomerOrderIdsAsync(PurchaseReceipt receipt, CancellationToken ct)
    {
        var receiptPoLineIds = receipt.Lines
            .Where(x => x.AcceptedQuantity > 0m)
            .Select(x => x.PurchaseOrderLineId)
            .ToHashSet();
        if (receiptPoLineIds.Count == 0)
            return [];

        var sources = await orderRepository.GetSourcesAsync(receipt.PurchaseOrderId, ct);
        var customerOrderIds = new HashSet<Guid>();
        foreach (var source in sources.Where(x => receiptPoLineIds.Contains(x.PurchaseOrderLineId)))
        {
            var purchaseRequest = await requestRepository.GetByLineIdAsync(source.PurchaseRequestLineId, ct);
            if (purchaseRequest?.CustomerOrderId is Guid customerOrderId && customerOrderId != Guid.Empty)
                customerOrderIds.Add(customerOrderId);
        }
        return customerOrderIds.ToArray();
    }

    private sealed record PostedReceiptWork(
        PurchaseReceiptPostResultDto Result,
        IReadOnlyList<Guid> CustomerOrderIds);
}
