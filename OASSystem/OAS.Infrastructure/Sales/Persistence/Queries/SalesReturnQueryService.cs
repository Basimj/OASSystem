using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Queries;

public sealed class SalesReturnQueryService(OAS.Infrastructure.Persistence.OasDbContext db) : ISalesReturnQueryService
{
    public async Task<SalesReturnDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Set<SalesReturn>().AsNoTracking().Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        return (await MapWithDirectoryAsync([entity], cancellationToken)).Single();
    }

    public async Task<SalesReturnsDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var grouped = await db.Set<SalesReturn>().AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var todayCount = await db.Set<SalesReturn>().AsNoTracking()
            .CountAsync(x => x.ReturnDate == today, cancellationToken);
        int Count(OAS.Domain.Sales.Enums.SalesReturnStatus status) =>
            grouped.FirstOrDefault(x => x.Status == status)?.Count ?? 0;
        return new SalesReturnsDashboardSummaryDto(grouped.Sum(x => x.Count), todayCount,
            Count(OAS.Domain.Sales.Enums.SalesReturnStatus.Draft),
            Count(OAS.Domain.Sales.Enums.SalesReturnStatus.Confirmed),
            Count(OAS.Domain.Sales.Enums.SalesReturnStatus.Posted),
            Count(OAS.Domain.Sales.Enums.SalesReturnStatus.Cancelled));
    }

    public async Task<PagedResult<SalesReturnDto>> GetPageAsync(
        PageRequest request,
        Guid? salesInvoiceId = null,
        Guid? customerId = null,
        SalesReturnStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = request.Normalize();
        var query = db.Set<SalesReturn>().AsNoTracking().AsQueryable();
        if (salesInvoiceId.HasValue) query = query.Where(x => x.SalesInvoiceId == salesInvoiceId.Value);
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (status.HasValue)
        {
            // Contracts and Domain enum types must not be mixed in an EF parameter.
            var domainStatus = (OAS.Domain.Sales.Enums.SalesReturnStatus)(byte)status.Value;
            query = query.Where(x => x.Status == domainStatus);
        }
        if (normalized.DateFrom.HasValue)
        {
            var from = normalized.DateFrom.Value;
            query = query.Where(x => x.ReturnDate >= from);
        }
        if (normalized.DateTo.HasValue)
        {
            var to = normalized.DateTo.Value;
            query = query.Where(x => x.ReturnDate <= to);
        }
        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search;
            var matchingInvoices = db.Set<SalesInvoice>().AsNoTracking()
                .Where(x => x.InvoiceCode.Contains(search)).Select(x => x.Id);
            var matchingCustomers = db.Set<Customer>().AsNoTracking()
                .Where(x => x.NameAr.Contains(search) || x.CustomerCode.Contains(search) ||
                    (x.NameEn != null && x.NameEn.Contains(search)) ||
                    (x.ContactInfo.Mobile != null && x.ContactInfo.Mobile.Contains(search)))
                .Select(x => x.Id);
            query = query.Where(x => x.ReturnCode.Contains(search) ||
                (x.Reason != null && x.Reason.Contains(search)) ||
                matchingInvoices.Contains(x.SalesInvoiceId) || matchingCustomers.Contains(x.CustomerId));
        }

        query = normalized.SortBy?.ToLowerInvariant() switch
        {
            "returncode" => normalized.SortDirection == SortDirection.Descending ? query.OrderByDescending(x => x.ReturnCode) : query.OrderBy(x => x.ReturnCode),
            "returndate" => normalized.SortDirection == SortDirection.Descending ? query.OrderByDescending(x => x.ReturnDate) : query.OrderBy(x => x.ReturnDate),
            "totalamount" => normalized.SortDirection == SortDirection.Descending ? query.OrderByDescending(x => x.TotalAmount) : query.OrderBy(x => x.TotalAmount),
            _ => query.OrderByDescending(x => x.ReturnDate).ThenByDescending(x => x.ReturnCode)
        };

        var total = await query.LongCountAsync(cancellationToken);
        // Load line collections after paging; never expand all return lines when counting.
        var items = await query.Skip((normalized.PageNumber - 1) * normalized.PageSize)
            .Take(normalized.PageSize).Include(x => x.Lines).AsSplitQuery().ToListAsync(cancellationToken);
        return new PagedResult<SalesReturnDto>
        {
            Items = await MapWithDirectoryAsync(items, cancellationToken),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = total
        };
    }

    private async Task<IReadOnlyList<SalesReturnDto>> MapWithDirectoryAsync(
        IReadOnlyList<SalesReturn> entities, CancellationToken ct)
    {
        if (entities.Count == 0) return [];
        var invoiceIds = entities.Select(x => x.SalesInvoiceId).Distinct().ToArray();
        var customerIds = entities.Select(x => x.CustomerId).Distinct().ToArray();
        var invoices = await db.Set<SalesInvoice>().AsNoTracking()
            .Where(x => invoiceIds.Contains(x.Id))
            .Select(x => new { x.Id, x.InvoiceCode }).ToDictionaryAsync(x => x.Id, ct);
        var customers = await db.Set<Customer>().AsNoTracking()
            .Where(x => customerIds.Contains(x.Id))
            .Select(x => new { x.Id, x.NameAr, x.CustomerCode }).ToDictionaryAsync(x => x.Id, ct);
        return entities.Select(entity =>
        {
            invoices.TryGetValue(entity.SalesInvoiceId, out var invoice);
            customers.TryGetValue(entity.CustomerId, out var customer);
            return Map(entity) with
            {
                SalesInvoiceCode = invoice?.InvoiceCode,
                CustomerName = customer?.NameAr,
                CustomerCode = customer?.CustomerCode
            };
        }).ToArray();
    }

    private static SalesReturnDto Map(SalesReturn entity) => new(
        entity.Id,
        entity.ReturnCode,
        entity.SalesInvoiceId,
        entity.CustomerId,
        entity.ReturnDate,
        entity.PostingDate,
        (SalesReturnStatus)(byte)entity.Status,
        entity.CurrencyId,
        entity.CurrencyCodeSnapshot,
        entity.CurrencyDecimalPlacesSnapshot,
        entity.ExchangeRate,
        entity.BaseCurrencyId,
        entity.BaseCurrencyCodeSnapshot,
        entity.BaseCurrencyDecimalPlacesSnapshot,
        entity.NetAmount,
        entity.TaxAmount,
        entity.TotalAmount,
        entity.BaseNetAmount,
        entity.BaseTaxAmount,
        entity.BaseTotalAmount,
        entity.Reason,
        entity.ReturnPolicyReferenceDate,
        entity.ReturnWindowDaysSnapshot,
        entity.ReturnWindowOverridden,
        entity.ReturnWindowOverrideReason,
        entity.ReturnWindowOverriddenBy,
        entity.JournalEntryId,
        entity.ConfirmedAtUtc,
        entity.PostedAtUtc,
        entity.CancelledAtUtc,
        Convert.ToBase64String(entity.RowVersion),
        entity.Lines.OrderBy(x => x.LineNumber).Select(x => new SalesReturnLineDto(
            x.Id,
            x.LineNumber,
            x.SalesInvoiceLineId,
            (OAS.Contracts.Sales.Enums.SalesLineType)(byte)x.LineType,
            x.ProductVariantId,
            x.WarehouseId,
            x.ProductCodeSnapshot,
            x.ProductNameSnapshot,
            x.Quantity,
            x.NetAmount,
            x.TaxAmount,
            x.FinalAmount,
            x.BaseNetAmount,
            x.BaseTaxAmount,
            x.BaseFinalAmount,
            x.UnitCostSnapshot,
            x.TotalCostSnapshot,
            x.ReturnCondition.HasValue ? (OAS.Contracts.Sales.Enums.SalesReturnCondition?)(byte)x.ReturnCondition.Value : null,
            x.StockDisposition.HasValue ? (OAS.Contracts.Sales.Enums.SalesReturnStockDisposition?)(byte)x.StockDisposition.Value : null,
            x.InspectedAtUtc,
            x.InspectedBy,
            Convert.ToBase64String(x.RowVersion))).ToList());
}
