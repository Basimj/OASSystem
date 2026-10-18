using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Returns.Services;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using DomainSalesReturnStatus = OAS.Domain.Sales.Enums.SalesReturnStatus;

namespace OAS.Application.Sales.Returns.Commands;

public sealed class ApplyCustomerReturnCreditCommandHandler(
    IRepository<CustomerCreditApplication, Guid> credits,
    IReadRepository<SalesReturn, Guid> returns,
    IReadRepository<SalesInvoice, Guid> invoices,
    IReadRepository<Customer, Guid> customers,
    ISalesInvoiceBalanceService invoiceBalances,
    ICustomerCreditConcurrencyPort locks,
    CustomerCreditLedgerService ledger,
    ICurrentUser actor,
    TimeProvider clock)
    : IRequestHandler<ApplyCustomerReturnCreditCommand, CustomerCreditSettlementDto>
{
    public async Task<CustomerCreditSettlementDto> Handle(ApplyCustomerReturnCreditCommand request,CancellationToken ct)
    {
        var d=request.Data;
        if (request.CustomerId==Guid.Empty || d.SalesReturnId==Guid.Empty ||
            d.TargetSalesInvoiceId==Guid.Empty || d.TargetInvoiceCurrencyAmount<=0)
            throw new ConflictException("customer_credit_input_invalid","بيانات تطبيق الرصيد الدائن غير صالحة.");
        // Lock return and invoice before reading either sum (also coordinates with SA-02 invoice locks).
        await locks.LockAsync(d.SalesReturnId,d.TargetSalesInvoiceId,ct);
        var key=Key(d.IdempotencyKey);
        if (key is not null)
        {
            var replay=(await credits.ListAsync(new Specification<CustomerCreditApplication>()
                .Where(x=>x.IdempotencyKey==key).ApplyPaging(0,2),ct)).FirstOrDefault();
            if (replay is not null)
            {
                if (replay.CustomerId!=request.CustomerId || replay.SalesReturnId!=d.SalesReturnId ||
                    replay.TargetSalesInvoiceId!=d.TargetSalesInvoiceId ||
                    replay.TargetInvoiceCurrencyAmount!=d.TargetInvoiceCurrencyAmount ||
                    replay.Status!=CustomerCreditApplicationStatus.Active)
                    throw new ConflictException("customer_credit_idempotency_conflict","مفتاح العملية مستخدم لعملية مختلفة أو معكوسة.");
                return CustomerCreditLedgerService.Map(replay);
            }
        }
        var source=await returns.GetByIdAsync(d.SalesReturnId,ct)
            ?? throw new NotFoundException(nameof(SalesReturn),d.SalesReturnId);
        var invoice=await invoices.GetByIdAsync(d.TargetSalesInvoiceId,ct)
            ?? throw new NotFoundException(nameof(SalesInvoice),d.TargetSalesInvoiceId);
        if (source.Status!=DomainSalesReturnStatus.Posted ||
            invoice.Status!=SalesInvoiceStatus.Posted ||
            source.CustomerId!=request.CustomerId || invoice.CustomerId!=request.CustomerId)
            throw new ConflictException("customer_credit_target_invalid","المرتجع والفاتورة يجب أن يكونا مرحلين ولنفس العميل.");
        if (invoice.Id==source.SalesInvoiceId)
            throw new ConflictException("customer_credit_original_invoice_not_allowed","المُرتجع يقلص فاتورته الأصلية تلقائيًا. اختر فاتورة أخرى.");
        var customer=await customers.GetByIdAsync(request.CustomerId,ct)
            ?? throw new NotFoundException(nameof(Customer),request.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException("customer_credit_customer_inactive","العميل غير نشط.");
        if (source.BaseCurrencyId!=invoice.BaseCurrencyId)
            throw new ConflictException("customer_credit_base_currency_mismatch","العملة الأساسية للمستندين يجب أن تتطابق.");

        var (sourceInfo,available)=await ledger.GetAvailableReturnAsync(request.CustomerId,source.Id,ct);
        var targetAmount=Math.Round(d.TargetInvoiceCurrencyAmount,invoice.CurrencyDecimalPlacesSnapshot,MidpointRounding.AwayFromZero);
        var baseValue=Math.Round(targetAmount*invoice.ExchangeRate,invoice.BaseCurrencyDecimalPlacesSnapshot,MidpointRounding.AwayFromZero);
        if (targetAmount<=0 || baseValue<=0 || baseValue>available+0.0001m)
            throw new ConflictException("customer_credit_insufficient","الرصيد الدائن المتاح لا يغطي قيمة التسوية.");
        var balance=await invoiceBalances.GetAsync(invoice,cancellationToken:ct);
        if (targetAmount>balance.OutstandingAmount+0.0001m || baseValue>balance.OutstandingBaseAmount+0.0001m)
            throw new ConflictException("customer_credit_target_overpaid","لا يمكن تجاوز المتبقي على فاتورة العميل.");
        var user=Actor(actor);
        var entity=CustomerCreditApplication.Apply(Guid.NewGuid(),source.Id,request.CustomerId,
            invoice.Id,baseValue,baseValue,targetAmount,user,clock.GetUtcNow().UtcDateTime,key);
        await credits.AddAsync(entity,ct);
        // A credit application is strictly a subledger allocation: original Posted Return
        // already credited AR and NO new journal is created here.
        return CustomerCreditLedgerService.Map(entity);
    }
    internal static string Actor(ICurrentUser user)
    {
        if (!Guid.TryParse(user.UserId,out var id) || id==Guid.Empty)throw new ForbiddenException();
        return id.ToString("D");
    }
    internal static string? Key(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))return null;
        var x=key.Trim();
        if (x.Length>80)throw new ConflictException("customer_credit_idempotency_key_invalid","مفتاح العملية يجب ألا يتجاوز 80 حرفًا.");
        return x;
    }
}

public sealed class RefundCustomerReturnCreditCommandHandler(
    IRepository<CustomerCreditApplication,Guid> credits,
    IReadRepository<SalesReturn,Guid> returns,
    IReadRepository<Customer,Guid> customers,
    IReadRepository<AccountingSettings,Guid> settings,
    IReadRepository<Currency,Guid> currencies,
    IRepository<PaymentVoucher,Guid> vouchers,
    IReadRepository<PaymentVoucherLine,Guid> voucherLines,
    ICustomerCreditConcurrencyPort locks,
    CustomerCreditLedgerService ledger,
    IVoucherSettlementResolver resolver,
    IAccountingDocumentPostingService posting,
    ISequenceNumberGenerator sequences,
    ICurrentUser actor,
    TimeProvider clock)
    : IRequestHandler<RefundCustomerReturnCreditCommand,CustomerCreditSettlementDto>
{
    public async Task<CustomerCreditSettlementDto> Handle(RefundCustomerReturnCreditCommand request,CancellationToken ct)
    {
        var d=request.Data;
        if (d.SalesReturnId==Guid.Empty || request.CustomerId==Guid.Empty || d.Amount<=0 || d.CurrencyId==Guid.Empty)
            throw new ConflictException("customer_refund_amount_invalid","بيانات الاسترداد غير صالحة.");
        var paymentMethod=(PaymentMethod)d.PaymentMethod;
        if (!Enum.IsDefined(paymentMethod) || paymentMethod==PaymentMethod.Other && !d.SettlementAccountId.HasValue)
            throw new ConflictException("customer_refund_method_invalid","طريقة الاسترداد أو حساب التسوية غير صالح.");
        await locks.LockAsync(d.SalesReturnId,null,ct);
        var key=ApplyCustomerReturnCreditCommandHandler.Key(d.IdempotencyKey);
        if (key is not null)
        {
            var replay=(await credits.ListAsync(new Specification<CustomerCreditApplication>()
                .Where(x=>x.IdempotencyKey==key).ApplyPaging(0,2),ct)).FirstOrDefault();
            if (replay is not null)
            {
                if (replay.CustomerId!=request.CustomerId || replay.SalesReturnId!=d.SalesReturnId ||
                    replay.SettlementKind!=CustomerCreditSettlementKind.RefundByPaymentVoucherLine ||
                    replay.Status!=CustomerCreditApplicationStatus.Active)
                    throw new ConflictException("customer_refund_idempotency_conflict","تم استخدام المفتاح لعملية أخرى أو تم عكسه.");
                // Verify retry is for the same source payment amount, not just the same key.
                var existingLine=await voucherLines.GetByIdAsync(replay.RefundPaymentVoucherLineId!.Value,ct);
                if (existingLine?.CurrencyId!=d.CurrencyId || existingLine?.Amount!=d.Amount ||
                    (byte?)existingLine?.PaymentMethod!=d.PaymentMethod ||
                    existingLine?.CashAccountId!=d.CashAccountId || existingLine?.BankAccountId!=d.BankAccountId)
                    throw new ConflictException("customer_refund_idempotency_conflict","بيانات طلب الإعادة تختلف عن الطلب الأصلي.");
                return CustomerCreditLedgerService.Map(replay);
            }
        }
        var source=await returns.GetByIdAsync(d.SalesReturnId,ct)
            ?? throw new NotFoundException(nameof(SalesReturn),d.SalesReturnId);
        if (source.Status!=DomainSalesReturnStatus.Posted || source.CustomerId!=request.CustomerId)
            throw new ConflictException("customer_refund_return_invalid","لا يمكن الاسترداد إلا من مرتجع مرحل للعميل نفسه.");
        var customer=await customers.GetByIdAsync(request.CustomerId,ct)
            ?? throw new NotFoundException(nameof(Customer),request.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException("customer_refund_customer_inactive","العميل غير نشط.");
        var acc=await settings.GetByIdAsync(AccountingSettings.SingletonId,ct)
            ?? throw new ConflictException("accounting_settings_required","إعدادات المحاسبة غير متاحة.");
        var baseCurrency=await currencies.GetByIdAsync(acc.BaseCurrencyId,ct)
            ?? throw new ConflictException("base_currency_missing","العملة الأساسية غير موجودة.");
        if (source.BaseCurrencyId!=baseCurrency.Id)
            throw new ConflictException("customer_refund_base_mismatch","العملة الأساسية التاريخية للمرتجع لا تطابق العملة الأساسية الحالية.");

        var date=DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var payment=await resolver.ResolveAsync(date,SettlementPartyType.Customer,request.CustomerId,
            null,null,null,null,paymentMethod,d.CashAccountId,d.BankAccountId,d.SettlementAccountId,
            d.CurrencyId,d.Amount,null,ExchangeRateType.Accounting,d.ReferenceNumber,ct);
        if (payment.CustomerId!=request.CustomerId || payment.CounterpartyAccountId!=customer.AccountId)
            throw new ConflictException("customer_refund_account_mismatch","الحساب المحاسبي لسند الصرف لا يطابق العميل.");
        var (_,available)=await ledger.GetAvailableReturnAsync(request.CustomerId,source.Id,ct);
        // Policy: posted return credit is an entitlement measured IN BASE CURRENCY.
        // Foreign-currency refunds consume current cash-base value; no fake FX is created.
        var consumed=Math.Round(payment.BaseAmount,source.BaseCurrencyDecimalPlacesSnapshot,MidpointRounding.AwayFromZero);
        if (consumed<=0 || consumed>available+0.0001m)
            throw new ConflictException("customer_refund_insufficient_credit","مبلغ الصرف يتجاوز الرصيد الدائن المتاح للمرتجع.");
        var user=ApplyCustomerReturnCreditCommandHandler.Actor(actor);
        var userId=Guid.Parse(user);
        var voucherId=Guid.NewGuid(); var lineId=Guid.NewGuid();
        var year=date.Year;
        var voucherNo=$"PV-{year:0000}-{await sequences.NextAsync($"PaymentVoucher-{year}",ct):000000}";
        var voucher=PaymentVoucher.CreateSettlementDocument(voucherId,voucherNo,date,
            baseCurrency.Id,baseCurrency.Code,baseCurrency.DecimalPlaces,payment.BaseAmount,
            $"رد رصيد دائن من مرتجع {source.ReturnCode}: {d.Description}");
        voucher.AddLine(PaymentVoucherLine.CreateSettlement(lineId,voucherId,1,
            payment.PartyType,payment.CustomerId,payment.SupplierId,payment.EmployeeId,
            payment.PartyNameSnapshot,payment.CounterpartyAccountId,payment.PaymentMethod,
            payment.CashAccountId,payment.BankAccountId,payment.SettlementAccountId,
            payment.CurrencyId,payment.CurrencyCode,payment.CurrencySymbol,payment.CurrencyDecimalPlaces,
            payment.Amount,payment.ExchangeRate,payment.ExchangeRateDate,payment.ExchangeRateType,
            payment.ExchangeRateSource,payment.BaseAmount,d.ReferenceNumber,date,
            "SalesReturn",source.Id,d.Description));
        await vouchers.AddAsync(voucher,ct);
        voucher.Approve(voucher.Lines);
        var now=clock.GetUtcNow().UtcDateTime;
        var journalId=await posting.PostPaymentVoucherAsync(voucher,voucher.Lines,userId,now,ct);
        voucher.SetJournalEntry(journalId);
        voucher.Post(userId,now,voucher.Lines);
        var application=CustomerCreditApplication.Refund(Guid.NewGuid(),source.Id,
            request.CustomerId,lineId,consumed,payment.BaseAmount,user,now,key);
        await credits.AddAsync(application,ct);
        return CustomerCreditLedgerService.Map(application);
    }
}

public sealed class ReverseCustomerReturnCreditCommandHandler(
    IRepository<CustomerCreditApplication,Guid> credits,
    IReadRepository<PaymentVoucherLine,Guid> lines,
    IReadRepository<ReceiptVoucherLine,Guid> receiptLines,
    IReadRepository<ReceiptVoucher,Guid> receipts,
    ICustomerCreditConcurrencyPort locks,
    ICurrentUser actor,
    TimeProvider clock)
    : IRequestHandler<ReverseCustomerReturnCreditCommand,CustomerCreditSettlementDto>
{
    public async Task<CustomerCreditSettlementDto> Handle(ReverseCustomerReturnCreditCommand request,CancellationToken ct)
    {
        var initial=await credits.GetByIdAsync(request.Id,ct)
            ?? throw new NotFoundException(nameof(CustomerCreditApplication),request.Id);
        if (initial.CustomerId!=request.CustomerId)
            throw new ConflictException("customer_credit_customer_mismatch","التخصيص لا يتبع العميل المحدد.");
        await locks.LockAsync(initial.SalesReturnId,initial.TargetSalesInvoiceId,ct);
        var item=await credits.GetForUpdateAsync(request.Id,ct)
            ?? throw new NotFoundException(nameof(CustomerCreditApplication),request.Id);
        if (item.Status==CustomerCreditApplicationStatus.Reversed)
            return CustomerCreditLedgerService.Map(item);
        // A posted refund has already debited AR/credited Cash. Require a genuine
        // posted compensating receipt, matched 1:1, before freeing the credit.
        if (item.SettlementKind==CustomerCreditSettlementKind.RefundByPaymentVoucherLine)
        {
            if (request.Data.CompensatingReceiptVoucherLineId is not Guid receiptId || receiptId==Guid.Empty)
                throw new ConflictException("customer_refund_compensating_receipt_required",
                    "يجب ربط سند قبض تصحيحي مرحل بنفس المبلغ قبل عكس الاسترداد.");
            await locks.LockReceiptAsync(receiptId,ct);
            var original=await lines.GetByIdAsync(item.RefundPaymentVoucherLineId!.Value,ct)
                ?? throw new NotFoundException(nameof(PaymentVoucherLine),item.RefundPaymentVoucherLineId.Value);
            var compensation=await receiptLines.GetByIdAsync(receiptId,ct)
                ?? throw new NotFoundException(nameof(ReceiptVoucherLine),receiptId);
            var receipt=await receipts.GetByIdAsync(compensation.ReceiptVoucherId,ct)
                ?? throw new NotFoundException(nameof(ReceiptVoucher),compensation.ReceiptVoucherId);
            if (receipt.Status!=ReceiptVoucherStatus.Posted || compensation.CustomerId!=item.CustomerId ||
                compensation.PartyType!=SettlementPartyType.Customer ||
                compensation.CounterpartyAccountId!=original.CounterpartyAccountId ||
                compensation.CurrencyId!=original.CurrencyId ||
                compensation.Amount!=original.Amount ||
                compensation.BaseAmount!=original.BaseAmount)
                throw new ConflictException("customer_refund_compensation_mismatch",
                    "سند القبض التصحيحي لا يطابق سند الصرف الأصلي في العميل والحساب والعملة والقيمة الأساسية.");
            var used=await credits.ListAsync(new Specification<CustomerCreditApplication>()
                .Where(x=>x.ReversalReceiptVoucherLineId==receiptId && x.Id!=item.Id),ct);
            if(used.Count>0)
                throw new ConflictException("customer_refund_compensation_reused",
                    "سند القبض التصحيحي مستخدم بالفعل لعكس استرداد آخر.");
        }
        else if (request.Data.CompensatingReceiptVoucherLineId.HasValue)
            throw new ConflictException("customer_credit_compensation_not_needed","عكس تطبيق الفاتورة لا يحتاج سند قبض.");
        string current=Convert.ToBase64String(item.RowVersion);
        if (string.IsNullOrWhiteSpace(request.Data.RowVersion) ||
            request.Data.RowVersion.Trim()!=current)
            throw new ConflictException("customer_credit_rowversion_conflict","تغيّرت التسوية؛ أعد تحميلها قبل العكس.");
        item.Reverse(clock.GetUtcNow().UtcDateTime,
            ApplyCustomerReturnCreditCommandHandler.Actor(actor),request.Data.Reason,
            request.Data.CompensatingReceiptVoucherLineId);
        credits.Update(item);
        return CustomerCreditLedgerService.Map(item);
    }
}
