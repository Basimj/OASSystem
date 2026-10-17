using Microsoft.EntityFrameworkCore;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Sales.Persistence.Queries;

public sealed class CommissionSourceQueryService(OasDbContext db) : ICommissionSourceQueryService
{
    public async Task<IReadOnlyList<CommissionSourceLine>> GetSourcesAsync(
        Guid employeeId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var invoiceRows = await (
            from i in db.Set<SalesInvoice>().AsNoTracking()
            join l in db.Set<SalesInvoiceLine>().AsNoTracking() on i.Id equals l.SalesInvoiceId
            join v0 in db.Set<ProductVariant>().AsNoTracking() on l.ProductVariantId equals v0.Id into vg
            from v in vg.DefaultIfEmpty()
            join p0 in db.Set<Product>().AsNoTracking() on v.ProductId equals p0.Id into pg
            from p in pg.DefaultIfEmpty()
            where i.Status == SalesInvoiceStatus.Posted && i.SalesEmployeeId == employeeId &&
                  i.PostingDate >= fromDate && i.PostingDate <= toDate && l.IsActive
            select new
            {
                Invoice = i,
                Line = l,
                CategoryId = p == null ? (Guid?)null : p.CategoryId
            }).ToListAsync(ct);

        var invoices = invoiceRows.Select(x => new CommissionSourceLine(
            "SalesInvoice", x.Invoice.Id, x.Line.Id, x.Invoice.Id, x.Line.Id, null,
            x.Invoice.PostingDate, x.Invoice.PostingDate,
            x.Line.BaseNetAmount,
            SalesPricingCalculator.ConvertToBase(
                SalesPricingCalculator.Round(x.Line.Quantity * x.Line.ActualUnitPrice, x.Invoice.CurrencyDecimalPlacesSnapshot),
                x.Invoice.ExchangeRate,
                x.Invoice.BaseCurrencyDecimalPlacesSnapshot),
            x.Line.BaseFinalAmount,
            x.Line.ProductVariantId,
            x.CategoryId,
            x.Line.LineType,
            false));

        var returnRows = await (
            from r in db.Set<SalesReturn>().AsNoTracking()
            join rl in db.Set<SalesReturnLine>().AsNoTracking() on r.Id equals rl.SalesReturnId
            join i in db.Set<SalesInvoice>().AsNoTracking() on r.SalesInvoiceId equals i.Id
            join ol in db.Set<SalesInvoiceLine>().AsNoTracking() on rl.SalesInvoiceLineId equals ol.Id
            join v0 in db.Set<ProductVariant>().AsNoTracking() on ol.ProductVariantId equals v0.Id into vg
            from v in vg.DefaultIfEmpty()
            join p0 in db.Set<Product>().AsNoTracking() on v.ProductId equals p0.Id into pg
            from p in pg.DefaultIfEmpty()
            where r.Status == SalesReturnStatus.Posted && i.SalesEmployeeId == employeeId &&
                  r.PostingDate >= fromDate && r.PostingDate <= toDate && rl.IsActive
            select new
            {
                Return = r,
                ReturnLine = rl,
                Invoice = i,
                OriginalLine = ol,
                CategoryId = p == null ? (Guid?)null : p.CategoryId
            }).ToListAsync(ct);

        var returns = returnRows.Select(x =>
        {
            var ratio = x.OriginalLine.Quantity <= 0m ? 0m : x.ReturnLine.Quantity / x.OriginalLine.Quantity;
            var originalGrossBase = SalesPricingCalculator.ConvertToBase(
                SalesPricingCalculator.Round(x.OriginalLine.Quantity * x.OriginalLine.ActualUnitPrice, x.Invoice.CurrencyDecimalPlacesSnapshot),
                x.Invoice.ExchangeRate,
                x.Invoice.BaseCurrencyDecimalPlacesSnapshot);
            var returnedGrossBase = decimal.Round(originalGrossBase * ratio, 4, MidpointRounding.AwayFromZero);
            return new CommissionSourceLine(
                "SalesReturn", x.Return.Id, x.ReturnLine.Id, x.Invoice.Id, x.ReturnLine.SalesInvoiceLineId, x.Return.Id,
                x.Return.PostingDate, x.Invoice.PostingDate,
                x.ReturnLine.BaseNetAmount,
                returnedGrossBase,
                x.ReturnLine.BaseFinalAmount,
                x.OriginalLine.ProductVariantId,
                x.CategoryId,
                x.ReturnLine.LineType,
                true);
        });

        return invoices.Concat(returns)
            .OrderBy(x => x.SourceDate)
            .ThenBy(x => x.SourceDocumentType)
            .ThenBy(x => x.SourceLineId)
            .ToList();
    }
}
