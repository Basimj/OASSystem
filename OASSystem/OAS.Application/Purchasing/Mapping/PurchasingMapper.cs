using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Contracts.Purchasing.PurchaseOrders;
using OAS.Contracts.Purchasing.PurchaseReceipts;
using OAS.Contracts.Purchasing.PurchaseRequests;
using OAS.Contracts.Purchasing.SupplierCatalog;
using OAS.Domain.Purchasing.Entities;
using ContractInvoiceStatus = OAS.Contracts.Purchasing.Enums.PurchaseInvoiceStatus;
using ContractOrderStatus = OAS.Contracts.Purchasing.Enums.PurchaseOrderStatus;
using ContractReceiptStatus = OAS.Contracts.Purchasing.Enums.PurchaseReceiptStatus;
using ContractRequestStatus = OAS.Contracts.Purchasing.Enums.PurchaseRequestStatus;
using ContractRequestType = OAS.Contracts.Purchasing.Enums.PurchaseRequestType;

namespace OAS.Application.Purchasing.Mapping;

public sealed class PurchasingMapper(
    IPurchasingReferenceDataPort references,
    IPurchaseRequestRepository requestRepository,
    IPurchaseOrderRepository orderRepository,
    IPurchaseInvoiceRepository invoiceRepository,
    ISupplierCatalogRepository catalogRepository)
{
    public async Task<SupplierCatalogItemDto> ToDtoAsync(SupplierCatalogItem item, CancellationToken cancellationToken)
    {
        var supplier = await references.GetSupplierAsync(item.SupplierId, cancellationToken);
        var product = await references.GetProductVariantAsync(item.ProductVariantId, cancellationToken);
        var unit = await references.GetUnitAsync(item.PurchaseUnitId, cancellationToken);
        var prices = await catalogRepository.GetPricesAsync(item.Id, cancellationToken);
        var priceDtos = new List<SupplierPriceHistoryDto>(prices.Count);
        foreach (var price in prices.OrderByDescending(x => x.EffectiveFrom))
        {
            var currency = await references.GetCurrencyAsync(price.CurrencyId, cancellationToken);
            priceDtos.Add(new SupplierPriceHistoryDto(price.Id, price.SupplierCatalogItemId, price.CurrencyId, currency?.Code,
                price.UnitPrice, price.EffectiveFrom, price.EffectiveTo, price.IsCurrent, price.Notes,
                PurchasingRowVersion.Encode(price.RowVersion), price.CreatedAtUtc, price.CreatedBy, price.LastModifiedAtUtc, price.LastModifiedBy));
        }

        return new SupplierCatalogItemDto(item.Id, item.SupplierId, supplier?.Code, supplier?.Name, item.ProductVariantId,
            product?.ProductCode, product?.ProductName, item.SupplierProductCode, item.SupplierProductName, item.PurchaseUnitId,
            unit?.Name, item.UnitConversionFactor, item.LeadTimeDays, item.MinimumOrderQuantity, item.IsPreferred, item.IsActive,
            priceDtos, PurchasingRowVersion.Encode(item.RowVersion), item.CreatedAtUtc, item.CreatedBy, item.LastModifiedAtUtc, item.LastModifiedBy);
    }

    public async Task<PurchaseRequestDto> ToDtoAsync(PurchaseRequest request, CancellationToken cancellationToken)
    {
        var warehouse = await references.GetWarehouseAsync(request.WarehouseId, cancellationToken);
        var allocated = await requestRepository.GetAllocatedQuantitiesAsync(request.Lines.Select(x => x.Id).ToArray(), cancellationToken);
        var lines = new List<PurchaseRequestLineDto>(request.Lines.Count);
        foreach (var line in request.Lines.OrderBy(x => x.LineSequence))
        {
            var product = await references.GetProductVariantAsync(line.ProductVariantId, cancellationToken);
            PurchasingSupplierSnapshot? preferredSupplier = null;
            if (line.PreferredSupplierId.HasValue)
                preferredSupplier = await references.GetSupplierAsync(line.PreferredSupplierId.Value, cancellationToken);
            allocated.TryGetValue(line.Id, out var allocatedQty);
            lines.Add(new PurchaseRequestLineDto(line.Id, line.PurchaseRequestId, line.LineSequence, line.ProductVariantId,
                product?.ProductCode, product?.ProductName, line.RequestedQuantity, allocatedQty,
                Math.Max(0m, line.RequestedQuantity - allocatedQty), line.RequiredDate, line.CustomerOrderLineId,
                line.PreferredSupplierId, preferredSupplier?.Name, line.Notes, PurchasingRowVersion.Encode(line.RowVersion)));
        }

        return new PurchaseRequestDto(request.Id, request.RequestCode, (ContractRequestType)(byte)request.RequestType,
            (ContractRequestStatus)(byte)request.Status, request.WarehouseId, warehouse?.Code, warehouse?.Name,
            request.CustomerOrderId, request.RequestDate, request.RequiredDate, request.Reason, request.Notes, request.RequestedBy,
            request.SubmittedBy, request.SubmittedAt, request.ApprovedBy, request.ApprovedAt, request.RejectedBy, request.RejectedAt,
            request.RejectionReason, request.CancelledBy, request.CancelledAt, request.CancellationReason, lines,
            PurchasingRowVersion.Encode(request.RowVersion), request.CreatedAtUtc, request.CreatedBy, request.LastModifiedAtUtc, request.LastModifiedBy);
    }

    public async Task<PurchaseOrderDto> ToDtoAsync(PurchaseOrder order, CancellationToken cancellationToken)
    {
        var supplier = await references.GetSupplierAsync(order.SupplierId, cancellationToken);
        var warehouse = await references.GetWarehouseAsync(order.DestinationWarehouseId, cancellationToken);
        var currency = await references.GetCurrencyAsync(order.CurrencyId, cancellationToken);
        var sources = await orderRepository.GetSourcesAsync(order.Id, cancellationToken);
        var received = await orderRepository.GetPostedReceivedBaseQuantitiesAsync(order.Id, cancellationToken);
        var lineDtos = new List<PurchaseOrderLineDto>(order.Lines.Count);
        foreach (var line in order.Lines.OrderBy(x => x.LineSequence))
        {
            received.TryGetValue(line.Id, out var receivedBase);
            var lineSources = sources.Where(x => x.PurchaseOrderLineId == line.Id)
                .Select(x => new PurchaseOrderLineSourceDto(x.Id, x.PurchaseOrderLineId, x.PurchaseRequestLineId, null,
                    x.AllocatedQuantity, PurchasingRowVersion.Encode(x.RowVersion))).ToArray();
            lineDtos.Add(new PurchaseOrderLineDto(line.Id, line.PurchaseOrderId, line.LineSequence, line.ProductVariantId,
                line.SupplierCatalogItemId, line.PurchaseUnitId, line.UnitConversionFactor, line.ProductCodeSnapshot,
                line.ProductNameSnapshot, line.UnitNameSnapshot, line.OrderedQuantity, line.BaseQuantity, receivedBase,
                Math.Max(0m, line.BaseQuantity - receivedBase), line.UnitPrice, line.DiscountAmount, line.NetAmount, line.TaxRate,
                line.TaxAmount, line.FinalAmount, line.ExpectedDeliveryDate, line.Notes, lineSources, PurchasingRowVersion.Encode(line.RowVersion)));
        }

        return new PurchaseOrderDto(order.Id, order.PurchaseOrderCode, order.SupplierId, supplier?.Code, supplier?.Name,
            order.DestinationWarehouseId, warehouse?.Code, warehouse?.Name, order.OrderDate, order.ExpectedDeliveryDate,
            order.CurrencyId, currency?.Code, order.ExchangeRate, order.ExchangeRateDate, (OAS.Contracts.Sales.Enums.TaxCalculationMode)(byte)order.TaxCalculationMode, (ContractOrderStatus)(byte)order.Status,
            order.Subtotal, order.DiscountAmount, order.TaxAmount, order.TotalAmount, order.PaymentTermDays, order.Notes,
            order.SubmittedBy, order.SubmittedAt, order.ApprovedBy, order.ApprovedAt, order.RejectedBy, order.RejectedAt,
            order.RejectionReason, order.SentBy, order.SentAt, order.ClosedBy, order.ClosedAt, order.CancelledBy, order.CancelledAt,
            order.CancellationReason, lineDtos, PurchasingRowVersion.Encode(order.RowVersion), order.CreatedAtUtc, order.CreatedBy,
            order.LastModifiedAtUtc, order.LastModifiedBy);
    }

    public async Task<PurchaseReceiptDto> ToDtoAsync(PurchaseReceipt receipt, CancellationToken cancellationToken)
    {
        var supplier = await references.GetSupplierAsync(receipt.SupplierId, cancellationToken);
        var warehouse = await references.GetWarehouseAsync(receipt.WarehouseId, cancellationToken);
        var order = await orderRepository.GetByIdAsync(receipt.PurchaseOrderId, cancellationToken);
        var lines = new List<PurchaseReceiptLineDto>(receipt.Lines.Count);
        foreach (var line in receipt.Lines.OrderBy(x => x.LineSequence))
        {
            var product = await references.GetProductVariantAsync(line.ProductVariantId, cancellationToken);
            lines.Add(new PurchaseReceiptLineDto(line.Id, line.PurchaseReceiptId, line.PurchaseOrderLineId, line.LineSequence,
                line.ProductVariantId, product?.ProductCode, product?.ProductName, line.OrderedQuantitySnapshot,
                line.PreviouslyReceivedQty, line.RemainingReceivableQuantity, line.ReceivedQuantity, line.AcceptedQuantity,
                line.RejectedQuantity, line.BaseAcceptedQuantity, line.ActualUnitCost, line.TotalAcceptedCost, line.ExpiryDate,
                line.BatchCode, line.Notes, PurchasingRowVersion.Encode(line.RowVersion), line.ReturnedQuantity));
        }

        return new PurchaseReceiptDto(receipt.Id, receipt.ReceiptCode, receipt.PurchaseOrderId, order?.PurchaseOrderCode,
            receipt.SupplierId, supplier?.Code, supplier?.Name, receipt.WarehouseId, warehouse?.Code, warehouse?.Name,
            receipt.ReceiptDate, receipt.PostingDate, receipt.SupplierDeliveryCode, (ContractReceiptStatus)(byte)receipt.Status,
            receipt.InventoryTransactionId, receipt.JournalEntryId, receipt.Notes, receipt.ConfirmedBy, receipt.ConfirmedAt,
            receipt.PostedBy, receipt.PostedAt, receipt.CancelledBy, receipt.CancelledAt, receipt.CancellationReason, lines,
            PurchasingRowVersion.Encode(receipt.RowVersion), receipt.CreatedAtUtc, receipt.CreatedBy, receipt.LastModifiedAtUtc,
            receipt.LastModifiedBy);
    }

    public async Task<PurchaseInvoiceDto> ToDtoAsync(PurchaseInvoice invoice, CancellationToken cancellationToken)
    {
        var supplier = await references.GetSupplierAsync(invoice.SupplierId, cancellationToken);
        var currency = await references.GetCurrencyAsync(invoice.CurrencyId, cancellationToken);
        var lines = invoice.Lines.OrderBy(x => x.LineSequence).Select(line => new PurchaseInvoiceLineDto(line.Id,
            line.PurchaseInvoiceId, line.LineSequence, line.PurchaseOrderLineId, line.ProductVariantId, line.ProductCodeSnapshot,
            line.DescriptionSnapshot, line.Quantity, line.UnitPrice, line.GrossAmount, line.DiscountAmount, line.NetAmount,
            line.TaxRate, line.TaxAmount, line.FinalAmount, line.BaseNetAmount, line.BaseTaxAmount, line.BaseFinalAmount,
            PurchasingRowVersion.Encode(line.RowVersion))).ToArray();

        return new PurchaseInvoiceDto(invoice.Id, invoice.PurchaseInvoiceCode, invoice.SupplierInvoiceCode, invoice.SupplierId,
            supplier?.Code, supplier?.Name, invoice.InvoiceDate, invoice.PostingDate, invoice.CurrencyId, currency?.Code,
            invoice.ExchangeRate, invoice.ExchangeRateDate, (OAS.Contracts.Sales.Enums.TaxCalculationMode)(byte)invoice.TaxCalculationMode, (ContractInvoiceStatus)(byte)invoice.Status, invoice.Subtotal,
            invoice.DiscountAmount, invoice.TaxAmount, invoice.TotalAmount, invoice.BaseSubtotal, invoice.BaseTaxAmount,
            invoice.BaseTotalAmount, invoice.JournalEntryId, invoice.Notes, invoice.ConfirmedBy, invoice.ConfirmedAt,
            invoice.PostedBy, invoice.PostedAt, invoice.CancelledBy, invoice.CancelledAt, invoice.CancellationReason, lines,
            PurchasingRowVersion.Encode(invoice.RowVersion), invoice.CreatedAtUtc, invoice.CreatedBy, invoice.LastModifiedAtUtc,
            invoice.LastModifiedBy);
    }

    public async Task<PurchaseMatchResultDto> ToMatchDtoAsync(PurchaseInvoice invoice, PurchaseMatchEvaluation evaluation, CancellationToken cancellationToken)
    {
        var stored = await invoiceRepository.GetAllocationsAsync(invoice.Id, cancellationToken);
        var allocations = evaluation.Allocations.Select(x =>
        {
            var persisted = stored.FirstOrDefault(p => p.PurchaseInvoiceLineId == x.PurchaseInvoiceLineId && p.PurchaseReceiptLineId == x.PurchaseReceiptLineId);
            return new PurchaseInvoiceReceiptAllocationDto(persisted?.Id ?? Guid.Empty, x.PurchaseInvoiceLineId, x.PurchaseReceiptLineId,
                x.ReceiptCode, x.MatchedQuantity, x.MatchedNetAmount, x.QuantityVariance, x.PriceVarianceAmount, x.TaxVarianceAmount,
                x.Status, persisted?.ApprovalReason, persisted?.ApprovedBy, persisted?.ApprovedAt,
                persisted is null ? string.Empty : PurchasingRowVersion.Encode(persisted.RowVersion));
        }).ToArray();

        var variances = evaluation.Allocations.SelectMany(x => BuildVariances(x)).ToArray();
        return new PurchaseMatchResultDto(invoice.Id, evaluation.OverallStatus, evaluation.RequiresApproval, allocations, variances);
    }

    private static IEnumerable<PurchaseVarianceDto> BuildVariances(PurchaseMatchAllocationEvaluation item)
    {
        if (item.PriceVarianceAmount != 0m)
            yield return new PurchaseVarianceDto(OAS.Contracts.Purchasing.Enums.PurchaseVarianceType.Price, item.PurchaseInvoiceLineId,
                item.PurchaseReceiptLineId, item.ReceiptCostBaseAmount, item.InvoiceNetBaseAmount, item.PriceVarianceAmount, item.PriceTolerance,
                item.Status, "فرق بين تكلفة الاستلام وصافي الفاتورة.");
        if (item.TaxVarianceAmount != 0m)
            yield return new PurchaseVarianceDto(OAS.Contracts.Purchasing.Enums.PurchaseVarianceType.Tax, item.PurchaseInvoiceLineId,
                item.PurchaseReceiptLineId, item.InvoiceTaxBaseAmount - item.TaxVarianceAmount, item.InvoiceTaxBaseAmount,
                item.TaxVarianceAmount, item.TaxTolerance, item.Status, "فرق ضريبة بين المستندات المطابقة.");
    }
}
