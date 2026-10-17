using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.PurchaseReturns;
using DomainPurchaseReturnStatus = OAS.Domain.Purchasing.Enums.PurchaseReturnStatus;

namespace OAS.Application.Purchasing.PurchaseReturns.Commands;

public sealed class PostPurchaseReturnCommandHandler(
    IPurchaseReturnRepository returns,
    IPurchaseReturnInventoryPostingService inventory,
    IPurchasingInventoryPort inventoryPort,
    IPurchasingAccountingPort accounting,
    ISupplierCreditSettlementService supplierCredits,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<PostPurchaseReturnCommand, PurchaseReturnPostingResultDto>
{
    public async Task<PurchaseReturnPostingResultDto> Handle(PostPurchaseReturnCommand command, CancellationToken ct)
    {
        var entity = await returns.GetForUpdateAsync(command.Id, ct)
            ?? throw new NotFoundException("PurchaseReturn", command.Id);
        PurchasingRowVersion.EnsureMatches(entity.RowVersion, command.Request.RowVersion, "Purchase return");
        if (entity.Status == DomainPurchaseReturnStatus.Posted && entity.JournalEntryId.HasValue)
            return new PurchaseReturnPostingResultDto(entity.Id, entity.JournalEntryId.Value, []);
        if (entity.Status != DomainPurchaseReturnStatus.Confirmed)
            throw new ConflictException("purchase_return_invalid_status", "يجب تأكيد مرتجع المشتريات قبل الترحيل.");
        if (string.IsNullOrWhiteSpace(currentUser.UserId))
            throw new ConflictException("purchase_return_user_required", "تعذر تحديد المستخدم المسؤول عن ترحيل مرتجع المشتريات.");

        await inventoryPort.ValidatePostingDateAsync(entity.WarehouseId, entity.PostingDate, ct);
        await accounting.ValidateSupplierAccountAsync(entity.SupplierId, ct);
        var now = timeProvider.GetUtcNow();
        var inventoryResult = await inventory.PostAsync(entity, currentUser.UserId, now, ct);
        var accountingResult = await accounting.PostPurchaseReturnJournalAsync(new PurchasingReturnAccountingContext(
            entity.Id,
            entity.ReturnCode,
            entity.PurchaseReceiptId,
            entity.PurchaseInvoiceId,
            entity.SupplierId,
            entity.PostingDate,
            entity.ReceiptCostBaseAmount,
            entity.SupplierNetBaseAmount,
            entity.SupplierTaxBaseAmount,
            entity.SupplierGrossBaseAmount,
            entity.InventoryCostBaseAmount,
            entity.PurchasePriceVarianceBaseAmount), ct);
        entity.MarkPosted(accountingResult.JournalEntryId, now, currentUser.UserId);
        returns.Update(entity);

        // A return linked to a posted supplier invoice has already debited Supplier AP in the
        // journal above. The following open-item application only settles that existing credit
        // against the original invoice; it must never create another journal entry. Any excess
        // remains visible as unapplied supplier credit.
        await supplierCredits.AutoApplyLinkedReturnAsync(
            entity,
            now.UtcDateTime,
            currentUser.UserId,
            ct);

        return new PurchaseReturnPostingResultDto(entity.Id, accountingResult.JournalEntryId, inventoryResult.InventoryTransactionIds);
    }
}
