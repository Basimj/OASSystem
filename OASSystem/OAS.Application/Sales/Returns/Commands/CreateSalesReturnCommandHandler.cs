using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Application.Sales.Returns.Commands;

public sealed class CreateSalesReturnCommandHandler(
    ISalesInvoiceAggregateRepository invoices,
    IRepository<SalesReturn, Guid> returns,
    IReadRepository<SalesReturnLine, Guid> existingReturnLines,
    IReadRepository<Warehouse, Guid> warehouses,
    IReadRepository<OpticalJob, Guid> opticalJobs,
    ISequenceNumberGenerator sequences,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
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
        if (request.Request.ReturnDate > request.Request.PostingDate)
            throw new ConflictException("sales_return_dates_invalid", "تاريخ المرتجع لا يمكن أن يكون بعد تاريخ الترحيل.");

        var invoice = await invoices.GetAggregateAsync(request.Request.SalesInvoiceId, true, ct)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.Request.SalesInvoiceId);
        if (invoice.Status != SalesInvoiceStatus.Posted)
            throw new ConflictException("sales_return_invoice_not_posted", "يمكن إنشاء مرتجع فقط لفاتورة مبيعات مرحلة.");
        if (request.Request.PostingDate < invoice.PostingDate)
            throw new ConflictException("sales_return_posting_date_invalid", "تاريخ ترحيل المرتجع لا يمكن أن يسبق تاريخ ترحيل الفاتورة الأصلية.");

        var policyReferenceDate = await ResolvePolicyReferenceDateAsync(invoice, ct);
        if (request.Request.ReturnDate < policyReferenceDate)
            throw new ConflictException("sales_return_before_delivery", "تاريخ المرتجع لا يمكن أن يسبق تاريخ التسليم/المرجع التشغيلي للفواتير المباشرة.");

        var windowEnd = policyReferenceDate.AddDays(SalesReturnPolicy.DefaultReturnWindowDays);
        var outsideWindow = request.Request.ReturnDate > windowEnd;
        if (outsideWindow)
        {
            if (!request.Request.OverrideReturnWindow)
                throw new ConflictException("sales_return_window_expired", $"انتهت مدة الإرجاع المسموح بها ({SalesReturnPolicy.DefaultReturnWindowDays} يومًا). يلزم تجاوز مصرح وسبب موثق.");
            if (string.IsNullOrWhiteSpace(request.Request.ReturnWindowOverrideReason))
                throw new ConflictException("sales_return_window_override_reason_required", "سبب تجاوز مدة الإرجاع مطلوب.");
            if (!await permissions.HasPermissionAsync(SalesPermissions.Returns.OverrideWindow, ct))
                throw new ForbiddenException("ليست لديك صلاحية تجاوز مدة الإرجاع.", SalesPermissions.Returns.OverrideWindow);
        }

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
        entity.SetReturnPolicySnapshot(
            policyReferenceDate,
            SalesReturnPolicy.DefaultReturnWindowDays,
            outsideWindow,
            outsideWindow ? request.Request.ReturnWindowOverrideReason : null,
            outsideWindow ? currentUser.UserId : null);

        var canOverrideQuality = await permissions.HasPermissionAsync(SalesPermissions.Returns.QualityOverride, ct);
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

            Guid? returnWarehouseId = null;
            SalesReturnCondition? returnCondition = null;
            SalesReturnStockDisposition? stockDisposition = null;

            if (source.RequiresInventory)
            {
                if (!requested.ReturnCondition.HasValue || requested.ReturnCondition == OAS.Contracts.Sales.Enums.SalesReturnCondition.Unknown ||
                    !requested.StockDisposition.HasValue || requested.StockDisposition == OAS.Contracts.Sales.Enums.SalesReturnStockDisposition.Unknown)
                    throw new ConflictException("sales_return_quality_required", "حدد حالة الصنف وقرار المخزون لكل سطر مخزني.");

                returnCondition = (SalesReturnCondition)(byte)requested.ReturnCondition.Value;
                stockDisposition = (SalesReturnStockDisposition)(byte)requested.StockDisposition.Value;

                if (stockDisposition is SalesReturnStockDisposition.Dispose or SalesReturnStockDisposition.NoStock)
                    throw new ConflictException("sales_return_direct_disposal_not_allowed", "الإتلاف المباشر عند إنشاء المرتجع غير مسموح؛ أعد الصنف إلى الحجر ثم سجّل قرار الفحص/الإتلاف بإجراء مخزني موثق.");

                var isCustomLens = source.LineType == SalesLineType.Lens && (source.RequiresProduction || source.PrescriptionSnapshot is not null);
                if (isCustomLens && (returnCondition == SalesReturnCondition.Resellable || stockDisposition == SalesReturnStockDisposition.Restock) && !canOverrideQuality)
                    throw new ForbiddenException("العدسة المصنعة/الوصفية يجب أن تدخل الحجر للفحص قبل السماح بإعادة بيعها.", SalesPermissions.Returns.QualityOverride);

                returnWarehouseId = requested.ReturnWarehouseId ?? source.WarehouseId;
                if (!returnWarehouseId.HasValue || returnWarehouseId == Guid.Empty)
                    throw new ConflictException("sales_return_warehouse_required", "حدد مخزن إرجاع لسطر المخزون.");

                var returnWarehouse = await warehouses.GetByIdAsync(returnWarehouseId.Value, ct)
                    ?? throw new ConflictException("sales_return_warehouse_missing", "مخزن الإرجاع المحدد غير موجود.");
                if (!returnWarehouse.IsActive)
                    throw new ConflictException("sales_return_warehouse_inactive", "مخزن الإرجاع المحدد غير مفعل.");

                if (stockDisposition == SalesReturnStockDisposition.Quarantine && returnWarehouse.AllowsSalesAllocation)
                    throw new ConflictException("sales_return_quarantine_warehouse_required", "قرار الحجر يتطلب مخزنًا نشطًا غير مسموح منه بتخصيص/بيع المخزون.");
                if (stockDisposition == SalesReturnStockDisposition.Restock && !returnWarehouse.AllowsSalesAllocation)
                    throw new ConflictException("sales_return_sellable_warehouse_required", "إعادة المخزون للبيع تتطلب مخزنًا مسموحًا منه بالتخصيص البيعي.");
            }
            else
            {
                if (requested.ReturnWarehouseId.HasValue)
                    throw new ConflictException("sales_return_warehouse_not_applicable", "مخزن الإرجاع يحدد فقط لأسطر المخزون.");
                stockDisposition = SalesReturnStockDisposition.NoStock;
            }

            var unitCost = source.RequiresInventory
                ? source.UnitCostSnapshot ?? throw new ConflictException("sales_return_cost_snapshot_missing", "تكلفة سطر الفاتورة الأصلية غير محفوظة ولا يمكن إنشاء المرتجع.")
                : (decimal?)null;
            decimal? totalCost = unitCost.HasValue ? Math.Round(unitCost.Value * requested.Quantity, 4) : null;

            try
            {
                entity.AddLine(SalesReturnLine.Create(
                    Guid.NewGuid(), entity.Id, lineNo++, source.Id, source.LineType,
                    source.ProductVariantId, returnWarehouseId, source.ProductCodeSnapshot, source.ProductNameSnapshot,
                    requested.Quantity, net, tax, Math.Round(net + tax, invoice.CurrencyDecimalPlacesSnapshot),
                    baseNet, baseTax, Math.Round(baseNet + baseTax, invoice.BaseCurrencyDecimalPlacesSnapshot),
                    unitCost, totalCost, returnCondition, stockDisposition));
            }
            catch (DomainException ex)
            {
                throw new ConflictException("sales_return_quality_invalid", ex.Message);
            }
        }

        await returns.AddAsync(entity, ct);
        return entity.Id;
    }

    private async Task<DateOnly> ResolvePolicyReferenceDateAsync(SalesInvoice invoice, CancellationToken ct)
    {
        var jobs = await opticalJobs.ListAsync(
            new Specification<OpticalJob>().Where(x => x.SalesInvoiceId == invoice.Id), ct);

        if (jobs.Count == 0)
            return invoice.PostingDate; // Direct/non-optical sale fallback documented by SA-01.

        var delivered = jobs
            .Where(x => x.DeliveredAtUtc.HasValue)
            .Select(x => x.DeliveredAtUtc!.Value)
            .ToArray();

        if (delivered.Length == 0)
            throw new ConflictException(
                "sales_return_not_delivered",
                "لا يمكن إنشاء مرتجع لفاتورة مرتبطة بأمر معمل لم يتم تسجيل تسليمه بعد.");

        var deliveredAt = delivered.Max();
        return DateOnly.FromDateTime(deliveredAt.UtcDateTime);
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
