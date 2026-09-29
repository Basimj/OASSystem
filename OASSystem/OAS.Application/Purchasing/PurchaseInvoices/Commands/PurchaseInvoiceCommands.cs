using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Common;
using OAS.Application.Purchasing.Mapping;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Domain.Purchasing.Entities;
using DomainInvoiceStatus = OAS.Domain.Purchasing.Enums.PurchaseInvoiceStatus;
using DomainMatchStatus = OAS.Domain.Purchasing.Enums.PurchaseMatchStatus;

namespace OAS.Application.Purchasing.PurchaseInvoices.Commands;

public sealed record CreatePurchaseInvoiceCommand(CreatePurchaseInvoiceRequest Request) : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Invoices.Create]; }
public sealed record UpdatePurchaseInvoiceCommand(Guid Id, UpdatePurchaseInvoiceRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Invoices.Edit]; }
public sealed record MatchPurchaseInvoiceCommand(Guid Id, MatchPurchaseInvoiceRequest Request) : ICommand<PurchaseMatchResultDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Invoices.Match]; }
public sealed record ConfirmPurchaseInvoiceCommand(Guid Id, ConfirmPurchaseInvoiceRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Invoices.Edit]; }
public sealed record ApprovePurchaseVarianceCommand(Guid InvoiceId, ApprovePurchaseVarianceRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Variance.Approve]; }
public sealed record RejectPurchaseVarianceCommand(Guid InvoiceId, RejectPurchaseVarianceRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Variance.Approve]; }
public sealed record PostPurchaseInvoiceCommand(Guid Id, PostPurchaseInvoiceRequest Request) : ICommand<PurchaseInvoicePostResultDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Invoices.Post]; }
public sealed record CancelPurchaseInvoiceCommand(Guid Id, CancelPurchaseInvoiceRequest Request) : ICommand, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; }=[PurchasingPermissions.Invoices.Cancel]; }

public sealed class CreatePurchaseInvoiceCommandHandler(
    IPurchaseInvoiceRepository invoiceRepository,
    IPurchaseOrderRepository orderRepository,
    IPurchasingReferenceDataPort references,
    IPurchasingCodeService codes) : IRequestHandler<CreatePurchaseInvoiceCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseInvoiceCommand command,CancellationToken ct)
    {
        var r=command.Request;
        PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(r.SupplierId,ct));
        PurchasingApplicationGuard.Currency(await references.GetCurrencyAsync(r.CurrencyId,ct));
        await EnsureSupplierInvoiceCodeUniqueAsync(invoiceRepository,r.SupplierId,r.SupplierInvoiceCode,null,ct);
        var id=Guid.NewGuid();
        var invoice=PurchaseInvoice.Create(id,await codes.NextPurchaseInvoiceCodeAsync(r.InvoiceDate,ct),r.SupplierInvoiceCode,r.SupplierId,
            r.InvoiceDate,r.PostingDate,r.CurrencyId,r.ExchangeRate,r.ExchangeRateDate,(OAS.Domain.Sales.Enums.TaxCalculationMode)(byte)r.TaxCalculationMode,r.Notes);
        foreach(var line in r.Lines.OrderBy(x=>x.LineSequence))
            invoice.AddLine(await BuildLineAsync(id,line.LineSequence,line.PurchaseOrderLineId,line.ProductVariantId,line.Quantity,line.UnitPrice,line.DiscountAmount,line.TaxRate,r.SupplierId,r.CurrencyId,r.ExchangeRate,(OAS.Domain.Sales.Enums.TaxCalculationMode)(byte)r.TaxCalculationMode,orderRepository,references,ct));
        await invoiceRepository.AddAsync(invoice,ct);
        return id;
    }

    internal static async Task<PurchaseInvoiceLine> BuildLineAsync(Guid invoiceId,int sequence,Guid? poLineId,Guid productVariantId,decimal quantity,
        decimal unitPrice,decimal discountAmount,decimal taxRate,Guid supplierId,Guid currencyId,decimal exchangeRate,OAS.Domain.Sales.Enums.TaxCalculationMode taxCalculationMode,
        IPurchaseOrderRepository orderRepository,IPurchasingReferenceDataPort references,CancellationToken ct,Guid? id=null)
    {
        var product=await references.GetProductVariantAsync(productVariantId,ct);PurchasingApplicationGuard.Product(product);
        if(poLineId.HasValue)
        {
            var order=await orderRepository.GetByLineIdAsync(poLineId.Value,ct)??throw new NotFoundException("PurchaseOrderLine",poLineId.Value);
            var poLine=order.Lines.Single(x=>x.Id==poLineId.Value);
            if(order.SupplierId!=supplierId)throw new ConflictException("purchasing_invoice_po_supplier_mismatch","سطر أمر الشراء يعود إلى مورد مختلف عن مورد الفاتورة.");
            if(order.CurrencyId!=currencyId)throw new ConflictException("purchasing_invoice_po_currency_mismatch","عملة أمر الشراء لا تطابق عملة فاتورة المورد.");
            if(poLine.ProductVariantId!=productVariantId)throw new ConflictException("purchasing_invoice_po_product_mismatch","المنتج لا يطابق سطر أمر الشراء المحدد.");
        }
        return PurchaseInvoiceLine.Create(id??Guid.NewGuid(),invoiceId,sequence,poLineId,productVariantId,product!.ProductCode,product.ProductName,
            quantity,unitPrice,discountAmount,taxRate,taxCalculationMode,exchangeRate);
    }

    internal static async Task EnsureSupplierInvoiceCodeUniqueAsync(IPurchaseInvoiceRepository repository,Guid supplierId,string? code,Guid? exceptId,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(code))return;
        if(await repository.SupplierInvoiceCodeExistsAsync(supplierId,code.Trim(),exceptId,ct))
            throw new ConflictException("purchasing_supplier_invoice_duplicate","فاتورة المورد مسجلة مسبقًا.");
    }
}

public sealed class UpdatePurchaseInvoiceCommandHandler(
    IPurchaseInvoiceRepository invoiceRepository,
    IPurchaseOrderRepository orderRepository,
    IPurchasingReferenceDataPort references) : IRequestHandler<UpdatePurchaseInvoiceCommand>
{
    public async Task Handle(UpdatePurchaseInvoiceCommand command,CancellationToken ct)
    {
        var invoice=await invoiceRepository.GetForUpdateAsync(command.Id,ct)??throw new NotFoundException("PurchaseInvoice",command.Id);
        PurchasingRowVersion.EnsureMatches(invoice.RowVersion,command.Request.RowVersion,"Purchase invoice");
        if(invoice.Status!=DomainInvoiceStatus.Draft)throw new ConflictException("purchasing_invoice_not_draft","يمكن تعديل فاتورة المورد عندما تكون مسودة فقط.");
        PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(command.Request.SupplierId,ct));
        PurchasingApplicationGuard.Currency(await references.GetCurrencyAsync(command.Request.CurrencyId,ct));
        await CreatePurchaseInvoiceCommandHandler.EnsureSupplierInvoiceCodeUniqueAsync(invoiceRepository,command.Request.SupplierId,command.Request.SupplierInvoiceCode,invoice.Id,ct);
        invoice.UpdateHeader(command.Request.SupplierInvoiceCode,command.Request.SupplierId,command.Request.InvoiceDate,command.Request.PostingDate,
            command.Request.CurrencyId,command.Request.ExchangeRate,command.Request.ExchangeRateDate,(OAS.Domain.Sales.Enums.TaxCalculationMode)(byte)command.Request.TaxCalculationMode,command.Request.Notes);

        var incomingIds=command.Request.Lines.Where(x=>x.Id.HasValue).Select(x=>x.Id!.Value).ToHashSet();
        foreach(var old in invoice.Lines.Where(x=>!incomingIds.Contains(x.Id)).ToArray())invoice.RemoveLine(old.Id);
        foreach(var line in command.Request.Lines.OrderBy(x=>x.LineSequence))
        {
            if(line.Id.HasValue)
            {
                var existing=invoice.Lines.SingleOrDefault(x=>x.Id==line.Id.Value)??throw new ConflictException("purchasing_invoice_line_missing","أحد بنود الفاتورة لم يعد موجودًا.");
                if(string.IsNullOrWhiteSpace(line.RowVersion))throw new ConcurrencyException("Purchase invoice line row version is required.");
                PurchasingRowVersion.EnsureMatches(existing.RowVersion,line.RowVersion,"Purchase invoice line");
                var structural=existing.LineSequence!=line.LineSequence||existing.PurchaseOrderLineId!=line.PurchaseOrderLineId||existing.ProductVariantId!=line.ProductVariantId;
                if(structural)
                {
                    invoice.RemoveLine(existing.Id);
                    invoice.AddLine(await CreatePurchaseInvoiceCommandHandler.BuildLineAsync(invoice.Id,line.LineSequence,line.PurchaseOrderLineId,line.ProductVariantId,
                        line.Quantity,line.UnitPrice,line.DiscountAmount,line.TaxRate,command.Request.SupplierId,command.Request.CurrencyId,command.Request.ExchangeRate,(OAS.Domain.Sales.Enums.TaxCalculationMode)(byte)command.Request.TaxCalculationMode,orderRepository,references,ct,existing.Id));
                }
                else existing.Update(line.Quantity,line.UnitPrice,line.DiscountAmount,line.TaxRate,invoice.TaxCalculationMode,command.Request.ExchangeRate);
            }
            else invoice.AddLine(await CreatePurchaseInvoiceCommandHandler.BuildLineAsync(invoice.Id,line.LineSequence,line.PurchaseOrderLineId,line.ProductVariantId,
                line.Quantity,line.UnitPrice,line.DiscountAmount,line.TaxRate,command.Request.SupplierId,command.Request.CurrencyId,command.Request.ExchangeRate,(OAS.Domain.Sales.Enums.TaxCalculationMode)(byte)command.Request.TaxCalculationMode,orderRepository,references,ct));
        }
        invoice.RecalculateTotals();
        await invoiceRepository.ReplaceLinesAsync(invoice,ct);
        invoiceRepository.Update(invoice);
    }
}

public sealed class MatchPurchaseInvoiceCommandHandler(
    IPurchaseInvoiceRepository invoiceRepository,
    IPurchaseMatchingService matching,
    PurchasingMapper mapper,
    IUnitOfWork unitOfWork) : IRequestHandler<MatchPurchaseInvoiceCommand,PurchaseMatchResultDto>
{
    public async Task<PurchaseMatchResultDto> Handle(MatchPurchaseInvoiceCommand command,CancellationToken ct)
    {
        var invoice=await invoiceRepository.GetForUpdateAsync(command.Id,ct)??throw new NotFoundException("PurchaseInvoice",command.Id);
        PurchasingRowVersion.EnsureMatches(invoice.RowVersion,command.Request.RowVersion,"Purchase invoice");
        var evaluation=await matching.EvaluateAsync(invoice,command.Request.Allocations,ct);
        var entities=evaluation.Allocations.Select(x=>PurchaseInvoiceReceiptAllocation.Create(Guid.NewGuid(),x.PurchaseInvoiceLineId,x.PurchaseReceiptLineId,
            x.MatchedQuantity,x.MatchedNetAmount,x.QuantityVariance,x.PriceVarianceAmount,x.TaxVarianceAmount,(DomainMatchStatus)(byte)x.Status)).ToArray();
        await invoiceRepository.ReplaceAllocationsAsync(invoice.Id,entities,ct);
        if(invoice.Status==DomainInvoiceStatus.Confirmed && evaluation.RequiresApproval)invoice.MarkMatchApprovalRequired();
        else if(invoice.Status==DomainInvoiceStatus.PendingMatchApproval && !evaluation.RequiresApproval)invoice.MarkMatchApproved();
        invoiceRepository.Update(invoice);
        // Persist the new allocations inside the current command transaction before mapping.
        // SQL Server generates RowVersion values on SaveChanges; variance approval needs those
        // values immediately in the match response. TransactionBehavior still owns commit/rollback.
        await unitOfWork.SaveChangesAsync(ct);
        return await mapper.ToMatchDtoAsync(invoice,evaluation,ct);
    }
}

public sealed class ConfirmPurchaseInvoiceCommandHandler(IPurchaseInvoiceRepository repository,ICurrentUser currentUser,TimeProvider timeProvider) : IRequestHandler<ConfirmPurchaseInvoiceCommand>
{
    public async Task Handle(ConfirmPurchaseInvoiceCommand command,CancellationToken ct)
    {
        var invoice=await repository.GetForUpdateAsync(command.Id,ct)??throw new NotFoundException("PurchaseInvoice",command.Id);
        PurchasingRowVersion.EnsureMatches(invoice.RowVersion,command.Request.RowVersion,"Purchase invoice");
        invoice.Confirm(timeProvider.GetUtcNow(),currentUser.UserId);
        var allocations=await repository.GetAllocationsAsync(invoice.Id,ct);
        if(allocations.Any(x=>x.MatchStatus==DomainMatchStatus.Rejected))throw new ConflictException("purchasing_match_rejected","توجد فروقات مطابقة مرفوضة.");
        if(allocations.Any(x=>x.MatchStatus==DomainMatchStatus.RequiresApproval))invoice.MarkMatchApprovalRequired();
        repository.Update(invoice);
    }
}

public sealed class ApprovePurchaseVarianceCommandHandler(IPurchaseInvoiceRepository repository,ICurrentUser currentUser,TimeProvider timeProvider) : IRequestHandler<ApprovePurchaseVarianceCommand>
{
    public async Task Handle(ApprovePurchaseVarianceCommand command,CancellationToken ct)
    {
        var invoice=await repository.GetForUpdateAsync(command.InvoiceId,ct)??throw new NotFoundException("PurchaseInvoice",command.InvoiceId);
        PurchasingRowVersion.EnsureMatches(invoice.RowVersion,command.Request.InvoiceRowVersion,"Purchase invoice");
        var allocation=await repository.GetAllocationForUpdateAsync(command.Request.AllocationId,ct)??throw new NotFoundException("PurchaseInvoiceReceiptAllocation",command.Request.AllocationId);
        if(invoice.Lines.All(x=>x.Id!=allocation.PurchaseInvoiceLineId))throw new NotFoundException("PurchaseInvoiceReceiptAllocation",command.Request.AllocationId);
        PurchasingRowVersion.EnsureMatches(allocation.RowVersion,command.Request.AllocationRowVersion,"Purchase match allocation");
        allocation.ApproveVariance(timeProvider.GetUtcNow(),currentUser.UserId,command.Request.ApprovalReason);
        repository.UpdateAllocation(allocation);
        var allocations=await repository.GetAllocationsAsync(invoice.Id,ct);
        var unresolved=allocations.Any(x=>x.Id!=allocation.Id && x.MatchStatus==DomainMatchStatus.RequiresApproval);
        var rejected=allocations.Any(x=>x.Id!=allocation.Id && x.MatchStatus==DomainMatchStatus.Rejected);
        if(invoice.Status==DomainInvoiceStatus.PendingMatchApproval && !unresolved && !rejected)invoice.MarkMatchApproved();
        repository.Update(invoice);
    }
}

public sealed class RejectPurchaseVarianceCommandHandler(IPurchaseInvoiceRepository repository) : IRequestHandler<RejectPurchaseVarianceCommand>
{
    public async Task Handle(RejectPurchaseVarianceCommand command,CancellationToken ct)
    {
        var invoice=await repository.GetForUpdateAsync(command.InvoiceId,ct)??throw new NotFoundException("PurchaseInvoice",command.InvoiceId);
        PurchasingRowVersion.EnsureMatches(invoice.RowVersion,command.Request.InvoiceRowVersion,"Purchase invoice");
        var allocation=await repository.GetAllocationForUpdateAsync(command.Request.AllocationId,ct)??throw new NotFoundException("PurchaseInvoiceReceiptAllocation",command.Request.AllocationId);
        if(invoice.Lines.All(x=>x.Id!=allocation.PurchaseInvoiceLineId))throw new NotFoundException("PurchaseInvoiceReceiptAllocation",command.Request.AllocationId);
        PurchasingRowVersion.EnsureMatches(allocation.RowVersion,command.Request.AllocationRowVersion,"Purchase match allocation");
        allocation.RejectVariance(command.Request.Reason);repository.UpdateAllocation(allocation);repository.Update(invoice);
    }
}

public sealed class PostPurchaseInvoiceCommandHandler(
    IPurchaseInvoiceRepository repository,
    IPurchaseMatchingService matching,
    IPurchasingReferenceDataPort references,
    IPurchasingAccountingPort accounting,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<PostPurchaseInvoiceCommand,PurchaseInvoicePostResultDto>
{
    public async Task<PurchaseInvoicePostResultDto> Handle(PostPurchaseInvoiceCommand command,CancellationToken ct)
    {
        var invoice=await repository.GetForUpdateAsync(command.Id,ct)??throw new NotFoundException("PurchaseInvoice",command.Id);
        if(invoice.Status==DomainInvoiceStatus.Posted && invoice.JournalEntryId.HasValue)return new(invoice.Id,invoice.JournalEntryId.Value,invoice.PurchaseInvoiceCode);
        PurchasingRowVersion.EnsureMatches(invoice.RowVersion,command.Request.RowVersion,"Purchase invoice");
        if(invoice.Status==DomainInvoiceStatus.PendingMatchApproval)throw new ConflictException("purchasing_invoice_variance_approval_required","يوجد فرق يحتاج إلى اعتماد قبل ترحيل الفاتورة.");
        if(invoice.Status!=DomainInvoiceStatus.Confirmed)throw new ConflictException("purchasing_invoice_not_confirmed","يجب تأكيد فاتورة المورد قبل الترحيل.");
        PurchasingApplicationGuard.Supplier(await references.GetSupplierAsync(invoice.SupplierId,ct));
        PurchasingApplicationGuard.Currency(await references.GetCurrencyAsync(invoice.CurrencyId,ct));
        await CreatePurchaseInvoiceCommandHandler.EnsureSupplierInvoiceCodeUniqueAsync(repository,invoice.SupplierId,invoice.SupplierInvoiceCode,invoice.Id,ct);
        await accounting.ValidatePostingPeriodAsync(invoice.PostingDate,ct);
        await accounting.ValidateSupplierAccountAsync(invoice.SupplierId,ct);

        var allocations=await repository.GetAllocationsAsync(invoice.Id,ct);
        if(allocations.Count==0)throw new ConflictException("purchasing_invoice_matching_required","لا يمكن ترحيل فاتورة المورد قبل اكتمال المطابقة.");
        if(allocations.Any(x=>x.MatchStatus is DomainMatchStatus.Pending or DomainMatchStatus.RequiresApproval or DomainMatchStatus.Rejected))
            throw new ConflictException("purchasing_invoice_matching_incomplete","لا يمكن ترحيل فاتورة المورد قبل اكتمال المطابقة واعتماد الفروقات.");
        var requested=allocations.Select(x=>new PurchaseInvoiceMatchAllocationRequest(x.PurchaseInvoiceLineId,x.PurchaseReceiptLineId,x.MatchedQuantity)).ToArray();
        var evaluation=await matching.EvaluateAsync(invoice,requested,ct);
        var priceVariance=Math.Round(invoice.BaseSubtotal-evaluation.ReceiptCostBaseAmount,4);
        var result=await accounting.PostPurchaseInvoiceJournalAsync(new(invoice.Id,invoice.PurchaseInvoiceCode,invoice.SupplierId,invoice.PostingDate,
            invoice.CurrencyId,invoice.ExchangeRate,evaluation.ReceiptCostBaseAmount,invoice.BaseTaxAmount,priceVariance,invoice.BaseTotalAmount),ct);
        invoice.MarkPosted(result.JournalEntryId,timeProvider.GetUtcNow(),currentUser.UserId);repository.Update(invoice);
        return new(invoice.Id,result.JournalEntryId,invoice.PurchaseInvoiceCode);
    }
}

public sealed class CancelPurchaseInvoiceCommandHandler(IPurchaseInvoiceRepository repository,ICurrentUser currentUser,TimeProvider timeProvider) : IRequestHandler<CancelPurchaseInvoiceCommand>
{
    public async Task Handle(CancelPurchaseInvoiceCommand command,CancellationToken ct){var invoice=await repository.GetForUpdateAsync(command.Id,ct)??throw new NotFoundException("PurchaseInvoice",command.Id);PurchasingRowVersion.EnsureMatches(invoice.RowVersion,command.Request.RowVersion,"Purchase invoice");invoice.Cancel(timeProvider.GetUtcNow(),currentUser.UserId,command.Request.Reason);repository.Update(invoice);}
}
