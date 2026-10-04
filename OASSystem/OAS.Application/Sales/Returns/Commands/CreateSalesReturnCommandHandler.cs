using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Returns.Commands;

public sealed class CreateSalesReturnCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    IRepository<SalesReturn, Guid> returns,
    IReadRepository<SalesReturnLine, Guid> existingReturnLines,
    ISequenceNumberGenerator sequences)
    : IRequestHandler<CreateSalesReturnCommand, Guid>
{
    public async Task<Guid> Handle(CreateSalesReturnCommand request, CancellationToken ct)
    {
        if (request.Request.SalesInvoiceId == Guid.Empty)
            throw new ConflictException("sales_return_invoice_required", "فاتورة المبيعات مطلوبة.");
        if (request.Request.Lines is null || request.Request.Lines.Count == 0)
            throw new ConflictException("sales_return_lines_required", "يجب إضافة سطر واحد على الأقل للمرتجع.");
        if (request.Request.Lines.GroupBy(x => x.SalesInvoiceLineId).Any(x => x.Count() > 1))
            throw new ConflictException("sales_return_line_duplicate", "لا يمكن تكرار سطر الفاتورة داخل نفس المرتجع.");

        var invoice = await invoices.GetAggregateAsync(request.Request.SalesInvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.Request.SalesInvoiceId);
        if (invoice.Status != SalesInvoiceStatus.Posted)
            throw new ConflictException("sales_return_invoice_not_posted", "يمكن إنشاء مرتجع فقط لفاتورة مبيعات مرحلة.");
        if (request.Request.PostingDate < invoice.PostingDate)
            throw new ConflictException("sales_return_posting_date_invalid", "تاريخ ترحيل المرتجع لا يمكن أن يسبق تاريخ ترحيل الفاتورة الأصلية.");

        var sequence = await sequences.NextAsync("SalesReturnCodeSequence", ct);
        var entity = SalesReturn.Create(
            Guid.NewGuid(),
            $"SR-{request.Request.ReturnDate.Year:0000}-{sequence:000000}",
            invoice.Id,
            invoice.CustomerId,
            request.Request.ReturnDate,
            request.Request.PostingDate,
            invoice.CurrencyId,
            invoice.CurrencyCodeSnapshot,
            invoice.CurrencyDecimalPlacesSnapshot,
            invoice.ExchangeRate,
            invoice.ExchangeRateDate,
            invoice.ExchangeRateType,
            invoice.ExchangeRateSource,
            invoice.BaseCurrencyId,
            invoice.BaseCurrencyCodeSnapshot,
            invoice.BaseCurrencyDecimalPlacesSnapshot,
            request.Request.Reason);

        var lineNo = 1;
        foreach (var requested in request.Request.Lines)
        {
            if (requested.SalesInvoiceLineId == Guid.Empty || requested.Quantity <= 0m)
                throw new ConflictException("sales_return_quantity_invalid", "سطر الفاتورة والكمية المرتجعة مطلوبان ويجب أن تكون الكمية أكبر من صفر.");

            var source = invoice.Lines.SingleOrDefault(x => x.Id == requested.SalesInvoiceLineId && x.IsActive)
                ?? throw new ConflictException("sales_return_source_line_missing", "أحد أسطر الفاتورة المحددة للمرتجع غير موجود أو غير فعال.");
            try
            {
                source.ReserveReturnQuantity(requested.Quantity);
            }
            catch (DomainException ex)
            {
                throw new ConflictException("sales_return_quantity_exceeded", ex.Message);
            }

            var existing = await existingReturnLines.ListAsync(
                new Specification<SalesReturnLine>().Where(x => x.SalesInvoiceLineId == source.Id && x.IsActive), ct);
            var isFinal = source.ReturnedQuantity == source.Quantity;
            var net = Allocate(source.NetAmount, existing.Sum(x => x.NetAmount), source.Quantity, requested.Quantity, invoice.CurrencyDecimalPlacesSnapshot, isFinal);
            var tax = Allocate(source.TaxAmount, existing.Sum(x => x.TaxAmount), source.Quantity, requested.Quantity, invoice.CurrencyDecimalPlacesSnapshot, isFinal);
            var baseNet = Allocate(source.BaseNetAmount, existing.Sum(x => x.BaseNetAmount), source.Quantity, requested.Quantity, invoice.BaseCurrencyDecimalPlacesSnapshot, isFinal);
            var baseTax = Allocate(source.BaseTaxAmount, existing.Sum(x => x.BaseTaxAmount), source.Quantity, requested.Quantity, invoice.BaseCurrencyDecimalPlacesSnapshot, isFinal);
            var unitCost = source.RequiresInventory
                ? source.UnitCostSnapshot ?? throw new ConflictException("sales_return_cost_snapshot_missing", "تكلفة سطر الفاتورة الأصلية غير محفوظة ولا يمكن إنشاء المرتجع.")
                : (decimal?)null;
            decimal? totalCost = unitCost.HasValue ? Math.Round(unitCost.Value * requested.Quantity, 4) : null;

            entity.AddLine(SalesReturnLine.Create(
                Guid.NewGuid(), entity.Id, lineNo++, source.Id, source.LineType,
                source.ProductVariantId, source.WarehouseId, source.ProductCodeSnapshot, source.ProductNameSnapshot,
                requested.Quantity, net, tax, Math.Round(net + tax, invoice.CurrencyDecimalPlacesSnapshot),
                baseNet, baseTax, Math.Round(baseNet + baseTax, invoice.BaseCurrencyDecimalPlacesSnapshot),
                unitCost, totalCost));
        }

        await returns.AddAsync(entity, ct);
        return entity.Id;
    }

    private static decimal Allocate(
        decimal originalTotal,
        decimal alreadyAllocated,
        decimal originalQuantity,
        decimal returnQuantity,
        byte decimals,
        bool isFinal)
    {
        if (originalTotal == 0m) return 0m;
        if (isFinal) return Math.Round(Math.Max(0m, originalTotal - alreadyAllocated), decimals);
        return Math.Round(originalTotal * returnQuantity / originalQuantity, decimals);
    }
}
