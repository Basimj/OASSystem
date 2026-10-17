using Microsoft.EntityFrameworkCore;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Reports;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Purchasing.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Persistence.Queries;

public sealed partial class SupplierPerformanceReportingQueryService
{
    private static IOrderedQueryable<LineSeed> ApplySort(IQueryable<LineSeed> query, string? sortBy, SortDirection direction)
    {
        var desc = direction == SortDirection.Descending;
        return (sortBy ?? "OrderDate").Trim().ToLowerInvariant() switch
        {
            "supplier" or "suppliername" => desc ? query.OrderByDescending(x => x.SupplierName) : query.OrderBy(x => x.SupplierName),
            "product" or "productname" => desc ? query.OrderByDescending(x => x.ProductName) : query.OrderBy(x => x.ProductName),
            "expecteddeliverydate" => desc ? query.OrderByDescending(x => x.ExpectedDeliveryDate) : query.OrderBy(x => x.ExpectedDeliveryDate),
            "purchaseordercode" => desc ? query.OrderByDescending(x => x.PurchaseOrderCode) : query.OrderBy(x => x.PurchaseOrderCode),
            _ => desc ? query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.PurchaseOrderCode)
                : query.OrderBy(x => x.OrderDate).ThenBy(x => x.PurchaseOrderCode)
        };
    }

    private DateOnly ResolveAsOf(SupplierPerformanceQueryRequest request) =>
        request.ToDate ?? DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    private static void ValidateDates(SupplierPerformanceQueryRequest request)
    {
        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
            throw new ArgumentException("FromDate cannot be after ToDate.");
    }

    private sealed class LineSeed
    {
        public Guid SupplierId { get; init; }
        public string SupplierCode { get; init; } = string.Empty;
        public string SupplierName { get; init; } = string.Empty;
        public Guid WarehouseId { get; init; }
        public string WarehouseCode { get; init; } = string.Empty;
        public string WarehouseName { get; init; } = string.Empty;
        public Guid CurrencyId { get; init; }
        public string CurrencyCode { get; init; } = string.Empty;
        public Guid ProductVariantId { get; init; }
        public string? ProductCode { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public Guid PurchaseOrderId { get; init; }
        public Guid PurchaseOrderLineId { get; init; }
        public string PurchaseOrderCode { get; init; } = string.Empty;
        public PurchaseOrderStatus Status { get; init; }
        public DateOnly OrderDate { get; init; }
        public DateOnly? ExpectedDeliveryDate { get; init; }
        public decimal OrderedQuantity { get; init; }
        public decimal BaseQuantity { get; init; }
        public decimal NetAmount { get; init; }
        public decimal ExchangeRate { get; init; }
    }

    private sealed class ReceiptActivity
    {
        public Guid PurchaseOrderLineId { get; init; }
        public Guid PurchaseReceiptId { get; init; }
        public string ReceiptCode { get; init; } = string.Empty;
        public DateOnly ReceiptDate { get; init; }
        public DateOnly PostingDate { get; init; }
        public decimal BaseAcceptedQuantity { get; init; }
        public decimal AcceptedQuantity { get; init; }
        public decimal ActualUnitCost { get; init; }
        public decimal TotalAcceptedCost { get; init; }
    }

    private sealed class ReturnActivity
    {
        public Guid PurchaseOrderLineId { get; init; }
        public Guid PurchaseReturnId { get; init; }
        public string ReturnCode { get; init; } = string.Empty;
        public DateOnly PostingDate { get; init; }
        public Guid? PurchaseInvoiceLineId { get; init; }
        public decimal BaseQuantity { get; init; }
        public decimal ReceiptCostBaseAmount { get; init; }
        public decimal SupplierGrossBaseAmount { get; init; }
    }

    private sealed class InvoiceActivity
    {
        public Guid PurchaseOrderLineId { get; init; }
        public Guid PurchaseInvoiceId { get; init; }
        public string PurchaseInvoiceCode { get; init; } = string.Empty;
        public DateOnly PostingDate { get; init; }
        public decimal MatchedQuantity { get; init; }
        public decimal MatchedNetAmount { get; init; }
        public decimal ExchangeRate { get; init; }
        public decimal BaseFinalAmount { get; init; }
        public decimal InvoiceLineQuantity { get; init; }
        public decimal ReceiptAcceptedQuantity { get; init; }
        public decimal ReceiptBaseAcceptedQuantity { get; init; }
        public decimal ReceiptActualUnitCost { get; init; }
    }
}
