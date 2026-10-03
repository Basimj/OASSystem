using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.Enums;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using AccountingPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;

namespace OAS.Application.Sales.Services;

public sealed class SalesImmediateSettlementService(
    IRepository<ReceiptVoucher, Guid> receiptVouchers,
    IRepository<PaymentAllocation, Guid> allocations,
    ICounterpartyAccountResolver counterparties,
    ISettlementAccountResolver settlementAccounts,
    IAccountingDocumentPostingService postingService,
    ISequenceNumberGenerator sequenceNumberGenerator,
    ISalesPaymentAllocationTargetValidator allocationValidator)
    : ISalesImmediateSettlementService
{
    public async Task<SalesImmediateSettlementResult> SettleAsync(
        SalesInvoice invoice,
        SalesImmediatePaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken ct = default)
    {
        if (invoice.TotalAmount <= 0m || invoice.BaseTotalAmount <= 0m)
            throw new ConflictException("sales_immediate_settlement_amount_invalid", "قيمة الفاتورة الفورية يجب أن تكون أكبر من صفر.");

        var accountingPaymentMethod = paymentMethod switch
        {
            SalesImmediatePaymentMethod.Cash => AccountingPaymentMethod.Cash,
            SalesImmediatePaymentMethod.Bank => AccountingPaymentMethod.BankTransfer,
            _ => throw new ConflictException("sales_immediate_payment_method_invalid", "طريقة تحصيل البيع الفوري غير صالحة.")
        };

        ValidateAccountSelection(paymentMethod, cashAccountId, bankAccountId);

        var party = await counterparties.ResolveAsync(
            SettlementPartyType.Customer,
            invoice.CustomerId,
            null,
            null,
            null,
            null,
            ct);

        var settlement = await settlementAccounts.ResolveAsync(
            accountingPaymentMethod,
            invoice.CurrencyId,
            cashAccountId,
            bankAccountId,
            null,
            invoice.InvoiceCode,
            ct);

        var voucherNumber = await ReserveVoucherNumberAsync(invoice.PostingDate.Year, ct);
        var voucherId = Guid.NewGuid();
        var lineId = Guid.NewGuid();

        var voucher = ReceiptVoucher.CreateSettlementDocument(
            voucherId,
            voucherNumber,
            invoice.PostingDate,
            invoice.BaseCurrencyId,
            invoice.BaseCurrencyCodeSnapshot,
            invoice.BaseCurrencyDecimalPlacesSnapshot,
            invoice.BaseTotalAmount,
            $"تحصيل فوري تلقائي لفاتورة المبيعات {invoice.InvoiceCode}");

        var line = ReceiptVoucherLine.CreateSettlement(
            lineId,
            voucherId,
            1,
            SettlementPartyType.Customer,
            invoice.CustomerId,
            null,
            null,
            party.PartyNameSnapshot,
            party.AccountId,
            accountingPaymentMethod,
            settlement.CashAccountId,
            settlement.BankAccountId,
            settlement.AccountId,
            invoice.CurrencyId,
            invoice.CurrencyCodeSnapshot,
            invoice.CurrencySymbolSnapshot,
            invoice.CurrencyDecimalPlacesSnapshot,
            invoice.TotalAmount,
            invoice.ExchangeRate,
            invoice.ExchangeRateDate,
            invoice.ExchangeRateType,
            invoice.ExchangeRateSource,
            invoice.BaseTotalAmount,
            invoice.InvoiceCode,
            invoice.InvoiceDate,
            "SalesInvoice",
            invoice.Id,
            $"تحصيل كامل للفاتورة {invoice.InvoiceCode}");

        voucher.AddLine(line);
        await receiptVouchers.AddAsync(voucher, ct);

        voucher.Approve(voucher.Lines);
        var receiptJournalId = await postingService.PostReceiptVoucherAsync(
            voucher,
            voucher.Lines,
            postedBy,
            postedAtUtc,
            ct);
        voucher.SetJournalEntry(receiptJournalId);
        voucher.Post(postedBy, postedAtUtc, voucher.Lines);
        await allocationValidator.ValidateAsync(
            invoice.Id,
            invoice.CurrencyId,
            invoice.TotalAmount,
            invoice.BaseTotalAmount,
            null,
            ct);

        var allocation = PaymentAllocation.CreateLineAllocation(
            Guid.NewGuid(),
            lineId,
            null,
            AllocationTargetDocumentType.SalesInvoice,
            invoice.Id,
            invoice.CurrencyId,
            invoice.CurrencyCodeSnapshot,
            invoice.TotalAmount,
            invoice.ExchangeRate,
            invoice.BaseTotalAmount,
            postedAtUtc);
        await allocations.AddAsync(allocation, ct);

        return new SalesImmediateSettlementResult(
            voucherId,
            voucherNumber,
            receiptJournalId,
            allocation.Id);
    }

    private static void ValidateAccountSelection(
        SalesImmediatePaymentMethod method,
        Guid? cashAccountId,
        Guid? bankAccountId)
    {
        if (method == SalesImmediatePaymentMethod.Cash)
        {
            if (!cashAccountId.HasValue || cashAccountId == Guid.Empty)
                throw new ConflictException("sales_immediate_cash_account_required", "يجب اختيار الصندوق قبل ترحيل الفاتورة الفورية النقدية.");
            if (bankAccountId.HasValue)
                throw new ConflictException("sales_immediate_bank_account_not_allowed", "لا يمكن تحديد حساب بنكي مع التحصيل النقدي.");
            return;
        }

        if (!bankAccountId.HasValue || bankAccountId == Guid.Empty)
            throw new ConflictException("sales_immediate_bank_account_required", "يجب اختيار الحساب البنكي قبل ترحيل الفاتورة الفورية البنكية.");
        if (cashAccountId.HasValue)
            throw new ConflictException("sales_immediate_cash_account_not_allowed", "لا يمكن تحديد صندوق مع التحصيل البنكي.");
    }

    private async Task<string> ReserveVoucherNumberAsync(int year, CancellationToken ct)
    {
        for (var i = 0; i < 100; i++)
        {
            var sequence = await sequenceNumberGenerator.NextAsync($"ReceiptVoucher-{year}", ct);
            var number = $"RV-{year:0000}-{sequence:000000}";
            var exists = await receiptVouchers.CountAsync(
                new Specification<ReceiptVoucher>().Where(x => x.VoucherNumber == number),
                ct);
            if (exists == 0)
                return number;
        }

        throw new ConflictException("receipt_voucher_number_duplicate", "تعذر حجز رقم فريد لسند القبض التلقائي.");
    }
}
