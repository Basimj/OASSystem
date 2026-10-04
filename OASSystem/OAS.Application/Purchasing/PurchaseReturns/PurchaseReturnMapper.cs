using OAS.Application.Abstractions.Persistence;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.PurchaseReturns;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.PurchaseReturns;

public static class PurchaseReturnMapper
{
    public static PurchaseReturnDto ToDto(PurchaseReturn entity) => new(
        entity.Id,
        entity.ReturnCode,
        entity.PurchaseReceiptId,
        entity.PurchaseInvoiceId,
        entity.SupplierId,
        entity.WarehouseId,
        entity.ReturnDate,
        entity.PostingDate,
        (PurchaseReturnStatus)(byte)entity.Status,
        entity.ReceiptCostBaseAmount,
        entity.SupplierNetBaseAmount,
        entity.SupplierTaxBaseAmount,
        entity.SupplierGrossBaseAmount,
        entity.InventoryCostBaseAmount,
        entity.PurchasePriceVarianceBaseAmount,
        entity.Reason,
        entity.JournalEntryId,
        entity.ConfirmedAt,
        entity.PostedAt,
        entity.CancelledAt,
        PurchasingRowVersion.Encode(entity.RowVersion),
        entity.Lines.OrderBy(x => x.LineNumber).Select(x => new PurchaseReturnLineDto(
            x.Id,
            x.LineNumber,
            x.PurchaseReceiptLineId,
            x.PurchaseInvoiceLineId,
            x.ProductVariantId,
            x.Quantity,
            x.BaseQuantity,
            x.ReceiptUnitCostBase,
            x.ReceiptCostBaseAmount,
            x.SupplierNetBaseAmount,
            x.SupplierTaxBaseAmount,
            x.SupplierGrossBaseAmount,
            x.InventoryUnitCostBase,
            x.InventoryCostBaseAmount)).ToList());

    public static PagedResult<PurchaseReturnDto> ToPage(PagedData<PurchaseReturn> page, PageRequest request)
    {
        var n = request.Normalize();
        return new PagedResult<PurchaseReturnDto>
        {
            Items = page.Items.Select(ToDto).ToList(),
            PageNumber = n.PageNumber,
            PageSize = n.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
