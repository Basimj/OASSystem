using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Queries;

public sealed class SalesReturnQueryService(OAS.Infrastructure.Persistence.OasDbContext db) : ISalesReturnQueryService
{
    public async Task<SalesReturnDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Set<SalesReturn>().AsNoTracking().Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<PagedResult<SalesReturnDto>> GetPageAsync(
        PageRequest request,
        Guid? salesInvoiceId = null,
        Guid? customerId = null,
        SalesReturnStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = request.Normalize();
        var query = db.Set<SalesReturn>().AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (salesInvoiceId.HasValue) query = query.Where(x => x.SalesInvoiceId == salesInvoiceId.Value);
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (status.HasValue)
        {
            var domainStatus = (OAS.Domain.Sales.Enums.SalesReturnStatus)(byte)status.Value;
            query = query.Where(x => x.Status == domainStatus);
        }
        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search = normalized.Search;
            query = query.Where(x => x.ReturnCode.Contains(search) || (x.Reason != null && x.Reason.Contains(search)));
        }

        query = normalized.SortBy?.ToLowerInvariant() switch
        {
            "returncode" => normalized.SortDirection == SortDirection.Descending ? query.OrderByDescending(x => x.ReturnCode) : query.OrderBy(x => x.ReturnCode),
            "returndate" => normalized.SortDirection == SortDirection.Descending ? query.OrderByDescending(x => x.ReturnDate) : query.OrderBy(x => x.ReturnDate),
            "totalamount" => normalized.SortDirection == SortDirection.Descending ? query.OrderByDescending(x => x.TotalAmount) : query.OrderBy(x => x.TotalAmount),
            _ => query.OrderByDescending(x => x.ReturnDate).ThenByDescending(x => x.ReturnCode)
        };

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip((normalized.PageNumber - 1) * normalized.PageSize).Take(normalized.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<SalesReturnDto>
        {
            Items = items.Select(Map).ToList(),
            PageNumber = normalized.PageNumber,
            PageSize = normalized.PageSize,
            TotalCount = total
        };
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
            x.TotalCostSnapshot)).ToList());
}
