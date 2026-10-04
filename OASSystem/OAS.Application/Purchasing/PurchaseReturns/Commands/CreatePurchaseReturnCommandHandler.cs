using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Contracts.Purchasing.PurchaseReturns;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.PurchaseReturns.Commands;

public sealed class CreatePurchaseReturnCommandHandler(
    IPurchaseReceiptRepository receipts,
    IPurchaseInvoiceRepository invoices,
    IPurchaseReturnRepository returns,
    IReadRepository<PurchaseReturnLine, Guid> returnLines,
    IPurchaseReturnDataPort dataPort,
    IPurchasingCodeService codes)
    : IRequestHandler<CreatePurchaseReturnCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseReturnCommand command, CancellationToken ct)
    {
        var request = command.Request;
        if (request.Lines is null || request.Lines.Count == 0)
            throw new ConflictException("purchase_return_lines_required", "يجب إضافة سطر واحد على الأقل لمرتجع المشتريات.");
        if (request.Lines.GroupBy(x => x.PurchaseReceiptLineId).Any(x => x.Count() > 1))
            throw new ConflictException("purchase_return_duplicate_receipt_line", "لا يمكن تكرار سطر الاستلام داخل نفس المرتجع.");

        var receipt = await receipts.GetForUpdateAsync(request.PurchaseReceiptId, ct)
            ?? throw new NotFoundException("PurchaseReceipt", request.PurchaseReceiptId);
        if (receipt.Status != PurchaseReceiptStatus.Posted)
            throw new ConflictException("purchase_return_receipt_not_posted", "يمكن إنشاء المرتجع فقط من استلام مشتريات مرحل.");
        if (request.PostingDate < receipt.PostingDate)
            throw new ConflictException("purchase_return_posting_date_invalid", "تاريخ ترحيل المرتجع لا يمكن أن يسبق تاريخ ترحيل الاستلام.");

        PurchaseInvoice? invoice = null;
        IReadOnlyList<PurchaseInvoiceReceiptAllocation> allocations = [];
        if (request.PurchaseInvoiceId.HasValue)
        {
            invoice = await invoices.GetByIdAsync(request.PurchaseInvoiceId.Value, ct)
                ?? throw new NotFoundException("PurchaseInvoice", request.PurchaseInvoiceId.Value);
            if (invoice.Status != PurchaseInvoiceStatus.Posted)
                throw new ConflictException("purchase_return_invoice_not_posted", "فاتورة المورد المرتبطة بالمرتجع يجب أن تكون مرحلة.");
            if (invoice.SupplierId != receipt.SupplierId)
                throw new ConflictException("purchase_return_supplier_mismatch", "مورد الفاتورة لا يطابق مورد الاستلام.");
            if (request.PostingDate < invoice.PostingDate)
                throw new ConflictException("purchase_return_before_invoice", "تاريخ ترحيل المرتجع لا يمكن أن يسبق تاريخ فاتورة المورد المرتبطة.");
            allocations = await invoices.GetAllocationsAsync(invoice.Id, ct);
        }

        var code = await codes.NextPurchaseReturnCodeAsync(request.ReturnDate, ct);
        var entity = PurchaseReturn.Create(Guid.NewGuid(), code, receipt.Id, invoice?.Id, receipt.SupplierId,
            receipt.WarehouseId, request.ReturnDate, request.PostingDate, request.Reason);

        var lineNumber = 1;
        foreach (var requested in request.Lines)
        {
            if (requested.PurchaseReceiptLineId == Guid.Empty || requested.Quantity <= 0m)
                throw new ConflictException("purchase_return_quantity_invalid", "سطر الاستلام والكمية المرتجعة مطلوبان ويجب أن تكون الكمية أكبر من صفر.");
            var source = receipt.Lines.SingleOrDefault(x => x.Id == requested.PurchaseReceiptLineId)
                ?? throw new ConflictException("purchase_return_receipt_line_missing", "أحد أسطر الاستلام المحددة غير موجود.");

            try { source.ReserveReturnQuantity(requested.Quantity); }
            catch (DomainException ex) { throw new ConflictException("purchase_return_quantity_exceeded", ex.Message); }

            Guid? invoiceLineId = null;
            decimal supplierNetBase = 0m;
            decimal supplierTaxBase = 0m;
            if (invoice is not null)
            {
                var allocation = allocations.SingleOrDefault(x => x.PurchaseReceiptLineId == source.Id)
                    ?? throw new ConflictException("purchase_return_invoice_allocation_missing", "سطر الاستلام غير مرتبط بفاتورة المورد المحددة.");
                var invoiceLine = invoice.Lines.SingleOrDefault(x => x.Id == allocation.PurchaseInvoiceLineId)
                    ?? throw new ConflictException("purchase_return_invoice_line_missing", "تعذر العثور على سطر فاتورة المورد المرتبط.");
                if (invoiceLine.ProductVariantId != source.ProductVariantId)
                    throw new ConflictException("purchase_return_product_mismatch", "المنتج في الفاتورة لا يطابق المنتج المستلم.");

                var previous = await returnLines.ListAsync(new Specification<PurchaseReturnLine>().Where(x =>
                    x.IsActive && x.PurchaseReceiptLineId == source.Id && x.PurchaseInvoiceLineId == invoiceLine.Id), ct);
                if (previous.Sum(x => x.Quantity) + requested.Quantity > allocation.MatchedQuantity)
                    throw new ConflictException("purchase_return_invoice_quantity_exceeded", "الكمية المرتجعة تتجاوز الكمية المطابقة لهذه الفاتورة.");

                invoiceLineId = invoiceLine.Id;
                supplierNetBase = Math.Round(invoiceLine.BaseNetAmount * requested.Quantity / invoiceLine.Quantity, 4);
                supplierTaxBase = Math.Round(invoiceLine.BaseTaxAmount * requested.Quantity / invoiceLine.Quantity, 4);
            }
            else if (await dataPort.HasPostedInvoiceAllocationAsync(source.Id, ct))
            {
                throw new ConflictException("purchase_return_invoice_required", "سطر الاستلام مرتبط بفاتورة مورد مرحلة؛ اختر الفاتورة حتى يتم عكس حساب المورد بشكل صحيح.");
            }

            var conversion = source.AcceptedQuantity == 0m ? 0m : source.BaseAcceptedQuantity / source.AcceptedQuantity;
            var baseQuantity = Math.Round(requested.Quantity * conversion, 3);
            entity.AddLine(PurchaseReturnLine.Create(
                Guid.NewGuid(), entity.Id, lineNumber++, source.Id, invoiceLineId, source.ProductVariantId,
                requested.Quantity, baseQuantity, source.ActualUnitCost, supplierNetBase, supplierTaxBase));
        }

        receipts.Update(receipt);
        await returns.AddAsync(entity, ct);
        return entity.Id;
    }
}
