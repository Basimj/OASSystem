using OAS.Application.Accounting.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using OAS.Application.Accounting.PaymentAllocations.Commands.ReversePaymentAllocation;
using OAS.Contracts.Accounting.PaymentAllocations;
using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Reports;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.Rules;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Accounting.Persistence.Queries;

public sealed partial class VoucherOperations(OasDbContext db, ISequenceNumberGenerator sequences, ICurrentUser user, TimeProvider clock, ISender sender, IPermissionChecker permissions) : IVoucherOperations
{
    private static ConflictException Error(string message) => new("voucher_operation_invalid", message);
    public async Task<Guid> ReverseAsync(bool receipt, Guid id, ReverseVoucherRequest request, CancellationToken ct)
    {
        var result = await ExecuteAsync(receipt, id, request, false, ct);
        return result.JournalId!.Value;
    }

    public async Task<VoucherReversalPreview> PreviewAsync(bool receipt, Guid id, ReverseVoucherRequest request, CancellationToken ct)
    {
        try { return await ExecuteAsync(receipt, id, request, true, ct); }
        catch (ConflictException ex) { return new(false, ex.Message, 0, 0, null); }
    }

    private async Task<VoucherReversalPreview> ExecuteAsync(bool receipt, Guid id, ReverseVoucherRequest request, bool preview, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 400 || request.Date == default)
            throw Error("حدد تاريخ العكس وسببًا لا يتجاوز 400 حرف.");
        if (!Guid.TryParse(user.UserId, out var actor) || actor == Guid.Empty) throw new ForbiddenException();
        await CashTransferService.LockAsync(db, ct);
        Guid? journalId;
        string number;
        List<Detail> details;
        if (receipt)
        {
            var v = await db.Set<ReceiptVoucher>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new NotFoundException(nameof(ReceiptVoucher),id);
            if (v.Status != ReceiptVoucherStatus.Posted) throw Error("لا يمكن عكس سند قبض غير مرحل.");
            journalId=v.JournalEntryId; number=v.VoucherNumber;
            details=await ReceiptDetails(db.Set<ReceiptVoucher>().Where(x=>x.Id==id)).ToListAsync(ct);
        }
        else
        {
            var v = await db.Set<PaymentVoucher>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new NotFoundException(nameof(PaymentVoucher),id);
            if (v.Status != PaymentVoucherStatus.Posted) throw Error("لا يمكن عكس سند صرف غير مرحل.");
            journalId=v.JournalEntryId; number=v.VoucherNumber;
            details=await PaymentDetails(db.Set<PaymentVoucher>().Where(x=>x.Id==id)).ToListAsync(ct);
        }
        if (details.Count == 0 || details.Any(x => !x.PartyType.HasValue || !x.SettlementId.HasValue || !x.CurrencyId.HasValue || !x.BaseAmount.HasValue || x.BaseAmount <= 0 || x.Amount <= 0 || (x.CashId.HasValue == x.BankId.HasValue)))
            throw Error("بيانات السند التاريخية أو بيانات التسوية غير مكتملة؛ لا يمكن عكسه من هذا المسار.");
        var ids = details.Select(x => x.LineId).ToArray();
        // Serialize against the existing allocation workflows, which lock source lines first.
        if (db.Database.IsSqlServer())
        {
            var table = receipt ? "tbl_ReceiptVoucherLines" : "tbl_PaymentVoucherLines";
            foreach (var lineId in ids.OrderBy(x => x))
            {
                await using var cmd = db.Database.GetDbConnection().CreateCommand();
                cmd.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
                cmd.CommandText = $"SELECT [Id] FROM [dbo].[{table}] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = @id";
                var parameter = cmd.CreateParameter(); parameter.ParameterName = "@id"; parameter.Value = lineId; cmd.Parameters.Add(parameter);
                await cmd.ExecuteScalarAsync(ct);
            }
        }
        var sourceType = receipt ? PaymentSourceType.ReceiptVoucher : PaymentSourceType.PaymentVoucher;
        var allocations = await db.Set<PaymentAllocation>().AsNoTracking().Where(x =>
            (x.ReceiptVoucherLineId.HasValue && ids.Contains(x.ReceiptVoucherLineId.Value)) ||
            (x.PaymentVoucherLineId.HasValue && ids.Contains(x.PaymentVoucherLineId.Value)) ||
            (x.PaymentSourceType == sourceType && (x.PaymentSourceId == id || ids.Contains(x.PaymentSourceId)))).ToListAsync(ct);
        var targetType = receipt ? AllocationTargetDocumentType.SalesInvoice : AllocationTargetDocumentType.PurchaseInvoice;
        var referenceType = receipt ? "SalesInvoice" : "PurchaseInvoice";
        foreach (var allocation in allocations)
        {
            var lineId = receipt ? allocation.ReceiptVoucherLineId : allocation.PaymentVoucherLineId;
            if (allocation.TargetDocumentType != targetType || !lineId.HasValue || !ids.Contains(lineId.Value) ||
                (receipt ? allocation.PaymentVoucherLineId.HasValue : allocation.ReceiptVoucherLineId.HasValue) || allocation.CustomerAdvanceApplicationId.HasValue)
                throw Error("يوجد تخصيص تاريخي أو تابع لعملية أخرى غير مدعومة؛ راجع العملية المصدر.");
            var line = details.Single(x => x.LineId == lineId.Value);
            if (line.PartyType != (int)(receipt ? SettlementPartyType.Customer : SettlementPartyType.Supplier))
                throw Error("نوع طرف التخصيص لا يطابق سند التحصيل أو السداد.");
        }
        foreach (var d in details)
        {
            if (d.ReferenceId.HasValue || !string.IsNullOrWhiteSpace(d.ReferenceType))
            {
                if (d.ReferenceType != referenceType || !d.ReferenceId.HasValue || !allocations.Any(a =>
                    (receipt ? a.ReceiptVoucherLineId : a.PaymentVoucherLineId) == d.LineId && a.TargetDocumentId == d.ReferenceId))
                    throw Error("السند مرتبط بمرجع تشغيلي لا يمكن عكسه من هذه الصفحة؛ استخدم العملية المصدر.");
            }
        }
        var activeAllocations = allocations.Where(a => !a.IsReversed).OrderBy(a => a.Id).ToArray();
        if (activeAllocations.Length > 0 && !await permissions.HasPermissionAsync(AccountingPermissions.PaymentAllocations.Edit, ct))
            throw Error("يتطلب عكس السند المرتبط بالفواتير صلاحية تعديل تخصيصات الدفعات.");
        // A refund/credit funded by the original invoice collection may cease to be
        // eligible if the collection is reversed. Use its own workflow first.
        var invoiceIds = activeAllocations.Select(a => a.TargetDocumentId).Distinct().ToArray();
        if (receipt && invoiceIds.Length > 0 && await (
            from application in db.Set<OAS.Domain.Sales.Entities.CustomerCreditApplication>()
            join returned in db.Set<OAS.Domain.Sales.Entities.SalesReturn>() on application.SalesReturnId equals returned.Id
            where invoiceIds.Contains(returned.SalesInvoiceId) && application.ReversedAtUtc == null
            select application.Id).AnyAsync(ct))
            throw Error("توجد تسوية ائتمانية أو رد نقدي لمرتجع فاتورة مرتبط بالتحصيل. عالج التسوية قبل عكس السند.");
        // Existing allocations record the effective removal using the actual UTC reversal timestamp.
        // Do not backdate their removal without a dedicated effective-date model/migration.
        if (activeAllocations.Length > 0 && request.Date != DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            throw Error("عكس سند ذي تخصيصات نشطة يتطلب تاريخ اليوم حتى تتطابق تقارير أعمار الذمم مع تاريخ إلغاء التخصيص.");
        if (activeAllocations.Length > 0 && request.Reason.Trim().Length > 300)
            throw Error("سبب العكس المرتبط بالتخصيصات يجب ألا يتجاوز 300 حرف.");
        if (await db.Set<CustomerAdvance>().AnyAsync(x => ids.Contains(x.ReceiptVoucherLineId), ct)
           || await db.Set<SupplierOpeningApplication>().AnyAsync(x=>ids.Contains(x.PaymentVoucherLineId),ct)
           || await db.Set<OAS.Domain.Sales.Entities.CustomerCreditApplication>().AnyAsync(x=>(x.RefundPaymentVoucherLineId.HasValue&&ids.Contains(x.RefundPaymentVoucherLineId.Value))||(x.ReversalReceiptVoucherLineId.HasValue&&ids.Contains(x.ReversalReceiptVoucherLineId.Value)),ct)
           || await db.Set<OAS.Domain.Features.Employees.Loans.EmployeeLoan>().AnyAsync(x=>x.PaymentVoucherId==id,ct)
           || await db.Set<OAS.Domain.Features.Employees.Loans.EmployeeLoanInstallment>().AnyAsync(x=>x.ReceiptVoucherId==id,ct))
            throw Error("السند مرتبط بعربون أو رصيد افتتاحي أو سلفة أو تسوية ائتمانية. يجب مراجعة العملية المصدر أولاً.");
        var original=await db.Set<JournalEntry>().Include(x=>x.Lines).SingleOrDefaultAsync(x=>x.Id==journalId,ct) ?? throw Error("القيد الأصلي غير موجود.");
        if(original.Status!=JournalEntryStatus.Posted || original.ReversedJournalId.HasValue || original.SourceDocumentId!=id || original.SourceDocumentType!=(receipt?"ReceiptVoucher":"PaymentVoucher") || original.JournalType!=JournalType.Automatic || request.Date<original.PostingDate)
            throw Error("القيد الأصلي غير صالح للعكس أو سبق عكسه أو تاريخ العكس غير صحيح.");
        if(original.BaseCurrencyId is not Guid baseId || original.BaseCurrencyDecimalPlacesSnapshot is not byte dp || string.IsNullOrWhiteSpace(original.BaseCurrencyCodeSnapshot)) throw Error("العملة الأساسية التاريخية غير متاحة.");
        var settings=await db.Set<AccountingSettings>().AsNoTracking().SingleAsync(x=>x.Id==AccountingSettings.SingletonId,ct);
        if(settings.BaseCurrencyId!=baseId) throw Error("تغيرت العملة الأساسية منذ ترحيل السند.");
        var journalLineIds=original.Lines.Select(x=>x.Id).ToArray();
        if(await db.Set<BankReconciliationMatch>().AnyAsync(x=>!x.IsVoided && ((x.JournalEntryLineId.HasValue&&journalLineIds.Contains(x.JournalEntryLineId.Value))||(x.ReceiptVoucherLineId.HasValue&&ids.Contains(x.ReceiptVoucherLineId.Value))||(x.PaymentVoucherLineId.HasValue&&ids.Contains(x.PaymentVoucherLineId.Value))),ct))
            throw Error("ألغِ المطابقة البنكية من مسار التسوية قبل العكس.");
        var accountIds=original.Lines.Select(x=>x.AccountId).Distinct().ToArray();
        var accounts=await db.Set<Account>().AsNoTracking().Where(x=>accountIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,ct);
        var cash=await db.Set<CashAccount>().AsNoTracking().Where(x=>accountIds.Contains(x.AccountId)).ToListAsync(ct);
        var banks=await db.Set<BankAccount>().AsNoTracking().Where(x=>accountIds.Contains(x.AccountId)).ToListAsync(ct);
        if (original.Lines.Count != details.Count * 2) throw Error("عدد بنود القيد الأصلي لا يطابق السند.");
        foreach (var l in original.Lines)
            if (!accounts.TryGetValue(l.AccountId, out var a) || !a.CanReceivePosting())
                throw Error($"الحساب {a?.Code} - {a?.NameAr} في القيد الأصلي غير صالح للترحيل.");
        foreach(var d in details)
        {
            var settlement=d.SettlementId!.Value;
            if (!accounts.TryGetValue(d.AccountId, out var other) || !other.CanReceivePosting() || settlement == d.AccountId || cash.Any(x=>x.AccountId==d.AccountId) || banks.Any(x=>x.AccountId==d.AccountId))
                throw Error($"حساب الطرف {other?.Code} - {other?.NameAr} غير صالح للعكس أو هو حساب تسوية نقدي.");
            // Match the historical counterparty against its current mapping, not a guessed account class.
            var kind = (SettlementPartyType)d.PartyType!.Value;
            var validParty = kind switch
            {
                SettlementPartyType.Customer => d.PartyId.HasValue && await db.Set<Customer>().AnyAsync(x => x.Id == d.PartyId && x.AccountId == d.AccountId, ct),
                SettlementPartyType.Supplier => d.PartyId.HasValue && await db.Set<Supplier>().AnyAsync(x => x.Id == d.PartyId && x.AccountId == d.AccountId, ct),
                SettlementPartyType.Employee => d.PartyId.HasValue && await db.Set<EmployeeAccount>().AnyAsync(x => x.EmployeeId == d.PartyId && x.AccountId == d.AccountId && x.IsActive, ct),
                SettlementPartyType.Other => !d.PartyId.HasValue && !other.IsControlAccount && !other.IsSystemAccount &&
                    !await db.Set<Customer>().AnyAsync(x => x.AccountId == d.AccountId, ct) &&
                    !await db.Set<Supplier>().AnyAsync(x => x.AccountId == d.AccountId, ct) &&
                    !await db.Set<EmployeeAccount>().AnyAsync(x => x.AccountId == d.AccountId, ct),
                _ => false
            };
            if (!validParty) throw Error($"ربط حساب الطرف {other.Code} - {other.NameAr} لا يطابق السند أو يتطلب مسارًا تشغيليًا خاصًا.");
            var count=cash.Count(x=>x.AccountId==settlement)+banks.Count(x=>x.AccountId==settlement);
            if(count!=1 || !(cash.Any(x=>x.Id==d.CashId&&x.AccountId==settlement&&x.CurrencyId==d.CurrencyId&&x.IsActive)||banks.Any(x=>x.Id==d.BankId&&x.AccountId==settlement&&x.CurrencyId==d.CurrencyId&&x.IsActive))) throw Error("تغير تعريف حساب التسوية أو عملته أو نشاطه.");
            var pair=original.Lines.Where(x=>x.SourceDocumentLineId==d.LineId).ToArray();
            if (pair.Length != 2 || pair.Any(x => x.CustomerId != (kind == SettlementPartyType.Customer ? d.PartyId : null) || x.SupplierId != (kind == SettlementPartyType.Supplier ? d.PartyId : null) || x.EmployeeId != (kind == SettlementPartyType.Employee ? d.PartyId : null)) ||
                !pair.Any(x=>x.AccountId==settlement&&x.TransactionCurrencyId==d.CurrencyId&&(receipt?x.DebitAmount:x.CreditAmount)==d.BaseAmount&&(receipt?x.TransactionDebitAmount:x.TransactionCreditAmount)==d.Amount) ||
                !pair.Any(x=>x.AccountId==d.AccountId&&x.TransactionCurrencyId==d.CurrencyId&&(receipt?x.CreditAmount:x.DebitAmount)==d.BaseAmount&&(receipt?x.TransactionCreditAmount:x.TransactionDebitAmount)==d.Amount))
                throw Error($"البند {d.LineNo} لا يطابق طرف أو مبالغ القيد الأصلي.");
            if (d.CashId is Guid cashId)
            {
                var shifts = await db.Set<CashShift>().AsNoTracking().Where(x => x.CashAccountId == cashId && (x.Status == CashShiftStatus.Open || x.Status == CashShiftStatus.Closing)).ToListAsync(ct);
                var box = cash.Single(x => x.Id == cashId);
                if (shifts.Count > 1 || shifts.Any(x => x.Status == CashShiftStatus.Closing) || (box.RequiresOpenShift && shifts.Count == 0))
                    throw Error($"الصندوق {box.Name}: افتح وردية صالحة أو أكمل الإغلاق قبل العكس.");
            }
        }
        if(receipt) foreach(var g in details.GroupBy(x=>new { Account=x.SettlementId!.Value, Currency=x.CurrencyId!.Value }))
        {
            var balance=await CashTransferService.ReadBalanceAsync(db,g.Key.Account,g.Key.Currency,baseId,request.Date,ct);
            if(balance.ErrorCode is not null) throw Error(balance.ErrorMessage!);
            if(balance.Native<g.Sum(x=>x.Amount)||balance.Base<g.Sum(x=>x.BaseAmount!.Value)) throw Error($"الرصيد المتاح لا يكفي لعكس سند القبض. المتاح: {balance.Native:0.####}، المطلوب: {g.Sum(x=>x.Amount):0.####} بعملة الحساب.");
        }
        var periods=await db.Set<FiscalPeriod>().Where(x=>x.StartDate<=request.Date&&x.EndDate>=request.Date).ToListAsync(ct);
        if(periods.Count!=1||!periods[0].CanPostAccounting()) throw Error("اختر فترة مالية مفتوحة للعكس.");
        // Validate known FX journals before entering the nested allocation commands.
        var allocationIds = activeAllocations.Select(x => x.Id).ToArray();
        var fxType = receipt ? "CustomerSettlementFx" : "SupplierSettlementFx";
        if (allocationIds.Length > 0)
        {
            var fx = await db.Set<JournalEntry>().AsNoTracking().Include(x => x.Lines).Where(x => x.SourceDocumentType == fxType && x.SourceDocumentId.HasValue && allocationIds.Contains(x.SourceDocumentId.Value)).ToListAsync(ct);
            foreach (var j in fx)
            {
                if (j.Status != JournalEntryStatus.Posted || j.BaseCurrencyId != baseId || j.PostingDate > request.Date || j.Lines.Count == 0 || !j.IsBalanced())
                    throw Error($"قيد فرق الصرف {j.JournalNumber} غير صالح للعكس في التاريخ المحدد.");
                var fxAccounts = j.Lines.Select(l => l.AccountId).Distinct().ToArray();
                var definitions = await db.Set<Account>().AsNoTracking().Where(x => fxAccounts.Contains(x.Id)).ToListAsync(ct);
                if (definitions.Count != fxAccounts.Length || definitions.Any(a => !a.CanReceivePosting()))
                    throw Error($"حساب في قيد فرق الصرف {j.JournalNumber} لا يقبل الترحيل.");
            }
        }
        if (preview) return new(true, null, details.Count, activeAllocations.Length, null);
        // Reuse the allocation command (including authorization, invoice locks, and FX reversal).
        // The nested unit of work uses the same database transaction. A failure rolls everything back.
        foreach (var allocation in activeAllocations)
            await sender.Send(new ReversePaymentAllocationCommand(allocation.Id,
                new ReversePaymentAllocationRequest(request.Date, request.Reason.Trim(), Convert.ToBase64String(allocation.RowVersion))), ct);
        var seq=await sequences.NextAsync($"JournalEntry-{request.Date.Year}",ct);
        var reversal=JournalEntry.Create(Guid.NewGuid(),$"JV-{request.Date.Year:0000}-{seq:000000}",JournalType.Reversal,request.Date,request.Date,periods[0].Id,$"عكس السند {number}: {request.Reason.Trim()}","Accounting",receipt?"ReceiptVoucherReversal":"PaymentVoucherReversal",id,JournalEntryStatus.Draft);
        reversal.SetBaseCurrencySnapshot(baseId,original.BaseCurrencyCodeSnapshot,dp);
        foreach(var line in original.Lines.OrderBy(x=>x.LineNumber)) reversal.AddLine(line.CreateOpposite(Guid.NewGuid(),reversal.Id));
        JournalPostingRules.EnsureBalanced(reversal.Lines); JournalPostingRules.EnsureCurrencySnapshots(reversal,reversal.Lines);
        var now=clock.GetUtcNow().UtcDateTime; reversal.SetPendingApproval(); reversal.Approve(actor,now); reversal.Post(actor,now);
        db.Add(reversal); original.MarkReversed(reversal.Id);
        return new(true, null, details.Count, activeAllocations.Length, reversal.Id);
    }
}
