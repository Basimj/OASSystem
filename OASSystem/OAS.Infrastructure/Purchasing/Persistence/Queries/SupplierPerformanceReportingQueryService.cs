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

public sealed partial class SupplierPerformanceReportingQueryService : ISupplierPerformanceReportingQueryService
{
    private readonly OasDbContext _db;
    private readonly TimeProvider _timeProvider;

    public SupplierPerformanceReportingQueryService(OasDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    private static readonly PurchaseOrderStatus[] ReportableOrderStatuses =
    [
        PurchaseOrderStatus.Sent,
        PurchaseOrderStatus.PartiallyReceived,
        PurchaseOrderStatus.FullyReceived,
        PurchaseOrderStatus.Closed
    ];

    public async Task<SupplierPerformanceSummaryDto> GetSummaryAsync(
        SupplierPerformanceQueryRequest request,
        bool includeCosts,
        CancellationToken cancellationToken = default)
    {
        ValidateDates(request);
        var asOf = ResolveAsOf(request);
        var baseQuery = ApplyOpenLateFilter(ApplyActivityWindowFilter(BuildSeedQuery(request), request), request, asOf);

        var orderedQuery = ApplyOrderDateWindow(baseQuery, request);
        var orderedBaseQuantity = await orderedQuery.SumAsync(x => (decimal?)x.BaseQuantity, cancellationToken) ?? 0m;

        var receiptActivity = BuildReceiptActivityQuery(request, baseQuery);
        var receivedBaseQuantity = await receiptActivity.SumAsync(x => (decimal?)x.BaseAcceptedQuantity, cancellationToken) ?? 0m;
        var receivedCostBaseAmount = includeCosts
            ? await receiptActivity.SumAsync(x => (decimal?)x.TotalAcceptedCost, cancellationToken) ?? 0m
            : 0m;

        var returnActivity = BuildReturnActivityQuery(request, baseQuery);
        var returnedBaseQuantity = await returnActivity.SumAsync(x => (decimal?)x.BaseQuantity, cancellationToken) ?? 0m;
        var purchaseReturnBaseAmount = includeCosts
            ? await returnActivity.SumAsync(x => (decimal?)x.SupplierGrossBaseAmount, cancellationToken) ?? 0m
            : 0m;

        var invoiceActivity = BuildPostedInvoiceActivityQuery(request, baseQuery);
        var invoicedNetBase = includeCosts
            ? await invoiceActivity.SumAsync(x => (decimal?)(x.MatchedNetAmount * x.ExchangeRate), cancellationToken) ?? 0m
            : 0m;
        var invoicedGrossBase = includeCosts
            ? await invoiceActivity.SumAsync(x => (decimal?)(x.InvoiceLineQuantity <= 0m
                ? 0m
                : x.BaseFinalAmount * x.MatchedQuantity / x.InvoiceLineQuantity), cancellationToken) ?? 0m
            : 0m;
        var matchedReceiptCostPeriod = includeCosts
            ? await invoiceActivity.SumAsync(x => (decimal?)(x.ReceiptAcceptedQuantity <= 0m
                ? 0m
                : x.MatchedQuantity * x.ReceiptBaseAcceptedQuantity / x.ReceiptAcceptedQuantity * x.ReceiptActualUnitCost), cancellationToken) ?? 0m
            : 0m;

        var openProjection = baseQuery.Select(x => new
        {
            x.PurchaseOrderId,
            x.PurchaseOrderLineId,
            x.BaseQuantity,
            x.ExpectedDeliveryDate,
            x.Status,
            AcceptedAsOf = (
                from rl in _db.Set<PurchaseReceiptLine>()
                join r in _db.Set<PurchaseReceipt>() on rl.PurchaseReceiptId equals r.Id
                where rl.PurchaseOrderLineId == x.PurchaseOrderLineId
                      && r.Status == PurchaseReceiptStatus.Posted
                      && r.PostingDate <= asOf
                select (decimal?)rl.BaseAcceptedQuantity).Sum() ?? 0m
        });

        var openQuery = openProjection.Select(x => new
        {
            Open = x.Status == PurchaseOrderStatus.Sent || x.Status == PurchaseOrderStatus.PartiallyReceived
                ? (x.BaseQuantity > x.AcceptedAsOf ? x.BaseQuantity - x.AcceptedAsOf : 0m)
                : 0m,
            x.PurchaseOrderId,
            x.ExpectedDeliveryDate
        });

        var openBaseQuantity = await openQuery.SumAsync(x => (decimal?)x.Open, cancellationToken) ?? 0m;
        var openLineCount = await openQuery.LongCountAsync(x => x.Open > 0m, cancellationToken);
        var openOrderCount = await openQuery.Where(x => x.Open > 0m).Select(x => x.PurchaseOrderId).Distinct().LongCountAsync(cancellationToken);
        var lateBaseQuantity = await openQuery
            .Where(x => x.Open > 0m && x.ExpectedDeliveryDate != null && x.ExpectedDeliveryDate < asOf)
            .SumAsync(x => (decimal?)x.Open, cancellationToken) ?? 0m;
        var lateLineCount = await openQuery.LongCountAsync(
            x => x.Open > 0m && x.ExpectedDeliveryDate != null && x.ExpectedDeliveryDate < asOf, cancellationToken);
        var lateOrderCount = await openQuery
            .Where(x => x.Open > 0m && x.ExpectedDeliveryDate != null && x.ExpectedDeliveryDate < asOf)
            .Select(x => x.PurchaseOrderId).Distinct().LongCountAsync(cancellationToken);
        var missingExpectedDateOpenLines = await openQuery.LongCountAsync(
            x => x.Open > 0m && x.ExpectedDeliveryDate == null, cancellationToken);

        var deliveryQuery =
            from receipt in receiptActivity
            join seed in baseQuery on receipt.PurchaseOrderLineId equals seed.PurchaseOrderLineId
            where receipt.BaseAcceptedQuantity > 0m
            select new
            {
                receipt.BaseAcceptedQuantity,
                LeadDays = EF.Functions.DateDiffDay(seed.OrderDate, receipt.ReceiptDate),
                seed.ExpectedDeliveryDate,
                receipt.ReceiptDate
            };

        var deliveryWeight = await deliveryQuery.SumAsync(x => (decimal?)x.BaseAcceptedQuantity, cancellationToken) ?? 0m;
        var leadWeightedDays = await deliveryQuery.SumAsync(
            x => (decimal?)(x.BaseAcceptedQuantity * x.LeadDays), cancellationToken) ?? 0m;
        var averageLead = deliveryWeight <= 0m
            ? (decimal?)null
            : Math.Round(leadWeightedDays / deliveryWeight, 2, MidpointRounding.AwayFromZero);
        var onTimeWeight = await deliveryQuery
            .Where(x => x.ExpectedDeliveryDate != null)
            .SumAsync(x => (decimal?)x.BaseAcceptedQuantity, cancellationToken) ?? 0m;
        var onTimeSuccessWeight = await deliveryQuery
            .Where(x => x.ExpectedDeliveryDate != null && x.ReceiptDate <= x.ExpectedDeliveryDate)
            .SumAsync(x => (decimal?)x.BaseAcceptedQuantity, cancellationToken) ?? 0m;
        var onTime = onTimeWeight <= 0m
            ? (decimal?)null
            : Math.Round(onTimeSuccessWeight * 100m / onTimeWeight, 2, MidpointRounding.AwayFromZero);

        decimal? grni = null;
        decimal? ppv = null;
        decimal? orderedBaseAmount = null;
        if (includeCosts)
        {
            orderedBaseAmount = await orderedQuery.SumAsync(x => (decimal?)(x.NetAmount * x.ExchangeRate), cancellationToken) ?? 0m;
            grni = await CalculateGrniAsOfAsync(baseQuery, asOf, cancellationToken);
            ppv = SupplierPerformanceMetrics.PurchasePriceVarianceBase(invoicedNetBase, matchedReceiptCostPeriod);
        }

        return new SupplierPerformanceSummaryDto(
            asOf,
            Math.Round(orderedBaseQuantity, 3),
            Math.Round(receivedBaseQuantity, 3),
            Math.Round(returnedBaseQuantity, 3),
            SupplierPerformanceMetrics.NetReceivedBaseQuantity(receivedBaseQuantity, returnedBaseQuantity),
            Math.Round(openBaseQuantity, 3),
            Math.Round(lateBaseQuantity, 3),
            openOrderCount,
            lateOrderCount,
            openLineCount,
            lateLineCount,
            missingExpectedDateOpenLines,
            orderedBaseAmount,
            includeCosts ? Math.Round(receivedCostBaseAmount, 4) : null,
            includeCosts ? Math.Round(invoicedNetBase, 4) : null,
            includeCosts ? Math.Round(invoicedGrossBase, 4) : null,
            includeCosts ? Math.Round(purchaseReturnBaseAmount, 4) : null,
            grni,
            ppv,
            averageLead,
            onTime);
    }
}
