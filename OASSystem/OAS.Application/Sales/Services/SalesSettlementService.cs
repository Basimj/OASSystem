using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Accounting.CustomerAdvances;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using ContractPaymentMethod = OAS.Contracts.Accounting.Enums.PaymentMethod;
using DomainPaymentMethod = OAS.Domain.Accounting.Enums.PaymentMethod;

namespace OAS.Application.Sales.Services;

public sealed class SalesSettlementService(
    IRepository<ReceiptVoucher, Guid> receiptVouchers,
    IReadRepository<ReceiptVoucherLine, Guid> receiptVoucherLines,
    IRepository<PaymentAllocation, Guid> allocations,
    IReadRepository<CustomerAdvance, Guid> advances,
    IReadRepository<PostingProfile, Guid> postingProfiles,
    IReadRepository<PostingProfileLine, Guid> postingProfileLines,
    IReadRepository<AccountingSettings, Guid> accountingSettings,
    IReadRepository<Currency, Guid> currencies,
    ICounterpartyAccountResolver counterparties,
    ISettlementAccountResolver settlementAccounts,
    IExchangeRateResolver exchangeRates,
    ICurrencyRoundingService rounding,
    IAccountingDocumentPostingService postingService,
    ICustomerAdvanceService customerAdvanceService,
    ISalesPaymentAllocationTargetValidator allocationValidator,
    ISequenceNumberGenerator sequenceNumberGenerator,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ISalesSettlementService
{
    public async Task<decimal> CalculatePaymentBaseAmountAsync(
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        DateOnly documentDate,
        CancellationToken cancellationToken = default)
    {
        if (paymentLines.Count == 0)
            return 0m;
        var baseCurrency = await GetBaseCurrencyAsync(cancellationToken);
        decimal total = 0m;
        foreach (var line in paymentLines)
        {
            ValidatePaymentLine(line);
            var rate = await exchangeRates.ResolveAsync(
                line.CurrencyId, documentDate, ExchangeRateType.Accounting, cancellationToken: cancellationToken);
            total += rounding.CalculateBaseAmount(
                line.Amount, rate.Rate, rate.CurrencyDecimalPlaces, baseCurrency.DecimalPlaces);
        }
        return rounding.Round(total, baseCurrency.DecimalPlaces);
    }

    public async Task<SalesPaymentCollectionResult> CreateReceiptForInvoiceAsync(
        SalesInvoice invoice,
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        if (invoice.Status != OAS.Domain.Sales.Enums.SalesInvoiceStatus.Posted)
            throw new ConflictException("sales_settlement_invoice_not_posted", "يجب ترحيل الفاتورة قبل إنشاء التحصيل.");
        if (paymentLines.Count == 0)
            throw new ConflictException("sales_payment_lines_required", "يجب إدخال طريقة دفع واحدة على الأقل.");
        var userId = GetCurrentUserId();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var party = await counterparties.ResolveAsync(
            SettlementPartyType.Customer, invoice.CustomerId, null, null, null, null, cancellationToken);
        var resolved = new List<ResolvedPaymentLine>(paymentLines.Count);
        foreach (var line in paymentLines)
            resolved.Add(await ResolvePaymentLineAsync(line, invoice.InvoiceDate, invoice.CustomerId, party.AccountId, party.PartyNameSnapshot, cancellationToken));

        var voucher = await CreateAndPostVoucherAsync(
            invoice.PostingDate,
            invoice.BaseCurrencyId,
            invoice.BaseCurrencyCodeSnapshot,
            invoice.BaseCurrencyDecimalPlacesSnapshot,
            resolved,
            $"تحصيل فاتورة المبيعات {invoice.InvoiceCode}",
            "SalesInvoice",
            invoice.Id,
            userId,
            now,
            cancellationToken);

        // The allocation validator reads the authoritative Posted invoice and previous allocations.
        // Persist the receipt first, still inside the outer transaction.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var allocationIds = new List<Guid>(resolved.Count);
        foreach (var item in resolved)
        {
            var validation = await allocationValidator.ValidateAsync(
                invoice.Id,
                item.CurrencyId,
                item.Amount,
                item.BaseAmount,
                null,
                cancellationToken);
            var allocation = PaymentAllocation.CreateLineAllocation(
                Guid.NewGuid(),
                item.LineId,
                null,
                AllocationTargetDocumentType.SalesInvoice,
                invoice.Id,
                item.CurrencyId,
                item.CurrencyCode,
                item.Amount,
                item.ExchangeRate,
                item.BaseAmount,
                now,
                validation.TargetBaseAllocatedAmount);
            await allocations.AddAsync(allocation, cancellationToken);
            allocationIds.Add(allocation.Id);
            // Make each allocation visible to the next cross-currency ceiling check.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new SalesPaymentCollectionResult(
            voucher.Id,
            voucher.VoucherNumber,
            resolved.Select(x => x.LineId).ToArray(),
            allocationIds,
            [],
            resolved.Sum(x => x.BaseAmount));
    }

    public async Task<SalesPaymentCollectionResult> CreateAdvanceForOrderAsync(
        CustomerOrder order,
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (paymentLines.Count == 0)
            throw new ConflictException("sales_payment_lines_required", "يجب إدخال طريقة دفع واحدة على الأقل.");
        var userId = GetCurrentUserId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var baseCurrency = await GetBaseCurrencyAsync(cancellationToken);
        var advanceLiabilityAccountId = await ResolveAdvanceLiabilityAccountAsync(cancellationToken);
        var party = await counterparties.ResolveAsync(
            SettlementPartyType.Customer, order.CustomerId, null, null, null, null, cancellationToken);

        var resolved = new List<ResolvedPaymentLine>(paymentLines.Count);
        foreach (var line in paymentLines)
            resolved.Add(await ResolvePaymentLineAsync(line, order.OrderDate, order.CustomerId, advanceLiabilityAccountId, party.PartyNameSnapshot, cancellationToken));

        var voucher = await CreateAndPostVoucherAsync(
            order.OrderDate,
            baseCurrency.Id,
            baseCurrency.Code,
            baseCurrency.DecimalPlaces,
            resolved,
            $"عربون طلب العميل {order.OrderCode}",
            "CustomerOrder",
            order.Id,
            userId,
            now,
            cancellationToken);

        // CustomerAdvanceService verifies the posted ReceiptVoucherLine, so persist the voucher
        // before creating the advance records. The outer checkout transaction still guarantees atomicity.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var advanceIds = new List<Guid>(resolved.Count);
        foreach (var item in resolved)
        {
            var advance = await customerAdvanceService.CreateAsync(
                new CreateCustomerAdvanceRequest(order.CustomerId, order.Id, item.LineId),
                cancellationToken);
            advanceIds.Add(advance.Id);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new SalesPaymentCollectionResult(
            voucher.Id,
            voucher.VoucherNumber,
            resolved.Select(x => x.LineId).ToArray(),
            [],
            advanceIds,
            resolved.Sum(x => x.BaseAmount));
    }

    public async Task<IReadOnlyList<Guid>> ApplyAdvancesToInvoiceAsync(
        CustomerOrder order,
        SalesInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        if (invoice.Status != OAS.Domain.Sales.Enums.SalesInvoiceStatus.Posted)
            throw new ConflictException("sales_advance_invoice_not_posted", "لا يمكن تطبيق العربون قبل ترحيل الفاتورة.");

        var rows = await advances.ListAsync(
            new Specification<CustomerAdvance>().Where(x => x.CustomerOrderId == order.Id && x.CustomerId == order.CustomerId && x.IsActive),
            cancellationToken);
        var available = rows
            .Where(x => x.AvailableAmount > 0m && x.BaseAvailableAmount > 0m)
            .OrderBy(x => x.ReceivedAtUtc)
            .ToArray();
        if (available.Length == 0)
            return [];

        var appliedIds = new List<Guid>();
        foreach (var advance in available)
        {
            var outstandingBase = await GetInvoiceOutstandingBaseAsync(invoice, cancellationToken);
            if (outstandingBase <= 0m)
                break;

            var baseToApply = Math.Min(advance.BaseAvailableAmount, outstandingBase);
            var sourceAmount = baseToApply == advance.BaseAvailableAmount
                ? advance.AvailableAmount
                : rounding.Round(baseToApply / advance.ExchangeRate, advance.CurrencyDecimalPlacesSnapshot);
            sourceAmount = Math.Min(advance.AvailableAmount, sourceAmount);
            if (sourceAmount <= 0m)
                continue;

            var result = await customerAdvanceService.ApplyAsync(
                advance.Id,
                new ApplyCustomerAdvanceRequest(
                    invoice.Id,
                    sourceAmount,
                    Convert.ToBase64String(advance.RowVersion)),
                cancellationToken);
            appliedIds.Add(result.Id);
            // Make the new allocation visible before calculating the next outstanding balance.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return appliedIds;
    }

    public async Task<SalesPaymentSummaryDto> GetPaymentSummaryAsync(
        CustomerOrder order,
        SalesInvoice? invoice,
        decimal paidNowAmount = 0m,
        CancellationToken cancellationToken = default)
    {
        var orderAdvances = await advances.ListAsync(
            new Specification<CustomerAdvance>().Where(x => x.CustomerOrderId == order.Id && x.CustomerId == order.CustomerId && x.IsActive),
            cancellationToken);
        var availableAdvanceBase = orderAdvances.Sum(x => Math.Max(0m, x.BaseAvailableAmount));
        var totalAdvanceBase = orderAdvances.Sum(x => Math.Max(0m, x.BaseAmount));
        var appliedAdvanceBase = totalAdvanceBase - availableAdvanceBase;

        if (invoice is null)
        {
            var availableInOrderCurrency = order.ExchangeRate <= 0m
                ? 0m
                : rounding.Round(availableAdvanceBase / order.ExchangeRate, order.CurrencyDecimalPlacesSnapshot);
            return new SalesPaymentSummaryDto(
                order.TotalAmount,
                paidNowAmount,
                availableInOrderCurrency,
                0m,
                0m,
                Math.Max(0m, order.TotalAmount - availableInOrderCurrency));
        }

        var allocationRows = await allocations.ListAsync(
            new Specification<PaymentAllocation>().Where(x =>
                x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice && x.TargetDocumentId == invoice.Id),
            cancellationToken);
        var totalAllocatedBase = allocationRows.Sum(GetBaseAllocatedAmount);
        var appliedFromAdvanceBase = allocationRows
            .Where(x => x.CustomerAdvanceApplicationId.HasValue)
            .Sum(GetBaseAllocatedAmount);
        var directPaidBase = Math.Max(0m, totalAllocatedBase - appliedFromAdvanceBase);
        var directPaid = invoice.ExchangeRate <= 0m
            ? 0m
            : rounding.Round(directPaidBase / invoice.ExchangeRate, invoice.CurrencyDecimalPlacesSnapshot);
        var totalSettled = invoice.ExchangeRate <= 0m
            ? 0m
            : rounding.Round(totalAllocatedBase / invoice.ExchangeRate, invoice.CurrencyDecimalPlacesSnapshot);
        var appliedAdvance = invoice.ExchangeRate <= 0m
            ? 0m
            : rounding.Round(appliedFromAdvanceBase / invoice.ExchangeRate, invoice.CurrencyDecimalPlacesSnapshot);
        var availableAdvance = invoice.ExchangeRate <= 0m
            ? 0m
            : rounding.Round(availableAdvanceBase / invoice.ExchangeRate, invoice.CurrencyDecimalPlacesSnapshot);

        return new SalesPaymentSummaryDto(
            invoice.TotalAmount,
            paidNowAmount,
            availableAdvance,
            appliedAdvance,
            Math.Min(invoice.TotalAmount, Math.Max(0m, directPaid)),
            Math.Max(0m, invoice.TotalAmount - totalSettled));
    }


    public async Task<SalesSettlementReferences> GetReferencesAsync(
        CustomerOrder order,
        SalesInvoice? invoice,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        var advanceRows = await advances.ListAsync(
            new Specification<CustomerAdvance>().Where(x =>
                x.CustomerOrderId == order.Id && x.CustomerId == order.CustomerId && x.IsActive),
            cancellationToken);

        var lineIds = new HashSet<Guid>(advanceRows.Select(x => x.ReceiptVoucherLineId));
        if (invoice is not null)
        {
            var invoiceAllocations = await allocations.ListAsync(
                new Specification<PaymentAllocation>().Where(x =>
                    x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice &&
                    x.TargetDocumentId == invoice.Id &&
                    x.ReceiptVoucherLineId.HasValue),
                cancellationToken);
            foreach (var allocation in invoiceAllocations)
                lineIds.Add(allocation.ReceiptVoucherLineId!.Value);
        }

        var receiptIds = lineIds.Count == 0
            ? []
            : (await receiptVoucherLines.ListAsync(
                new Specification<ReceiptVoucherLine>().Where(x => lineIds.Contains(x.Id)),
                cancellationToken))
                .Select(x => x.ReceiptVoucherId)
                .Distinct()
                .ToArray();

        return new SalesSettlementReferences(
            receiptIds,
            advanceRows.Select(x => x.Id).Distinct().ToArray());
    }

    private async Task<ReceiptVoucher> CreateAndPostVoucherAsync(
        DateOnly voucherDate,
        Guid baseCurrencyId,
        string baseCurrencyCode,
        byte baseCurrencyDecimalPlaces,
        IReadOnlyList<ResolvedPaymentLine> resolved,
        string description,
        string referenceType,
        Guid referenceId,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken)
    {
        var voucherId = Guid.NewGuid();
        var voucherNumber = await ReserveVoucherNumberAsync(voucherDate.Year, cancellationToken);
        var baseTotal = rounding.Round(resolved.Sum(x => x.BaseAmount), baseCurrencyDecimalPlaces);
        var voucher = ReceiptVoucher.CreateSettlementDocument(
            voucherId,
            voucherNumber,
            voucherDate,
            baseCurrencyId,
            baseCurrencyCode,
            baseCurrencyDecimalPlaces,
            baseTotal,
            description);

        foreach (var item in resolved)
        {
            voucher.AddLine(ReceiptVoucherLine.CreateSettlement(
                item.LineId,
                voucherId,
                1,
                SettlementPartyType.Customer,
                item.CustomerId,
                null,
                null,
                item.PartyName,
                item.CounterpartyAccountId,
                item.PaymentMethod,
                item.CashAccountId,
                item.BankAccountId,
                item.SettlementAccountId,
                item.CurrencyId,
                item.CurrencyCode,
                item.CurrencySymbol,
                item.CurrencyDecimalPlaces,
                item.Amount,
                item.ExchangeRate,
                item.ExchangeRateDate,
                item.ExchangeRateType,
                item.ExchangeRateSource,
                item.BaseAmount,
                item.ReferenceNumber,
                item.ReferenceDate,
                referenceType,
                referenceId,
                item.Description));
        }

        await receiptVouchers.AddAsync(voucher, cancellationToken);
        voucher.Approve(voucher.Lines);
        var journalId = await postingService.PostReceiptVoucherAsync(
            voucher, voucher.Lines, postedBy, postedAtUtc, cancellationToken);
        voucher.SetJournalEntry(journalId);
        voucher.Post(postedBy, postedAtUtc, voucher.Lines);
        return voucher;
    }

    private async Task<ResolvedPaymentLine> ResolvePaymentLineAsync(
        CheckoutPaymentLineRequest line,
        DateOnly documentDate,
        Guid customerId,
        Guid counterpartyAccountId,
        string partyName,
        CancellationToken cancellationToken)
    {
        ValidatePaymentLine(line);
        var paymentMethod = (DomainPaymentMethod)(byte)line.PaymentMethod;
        var settlement = await settlementAccounts.ResolveAsync(
            paymentMethod,
            line.CurrencyId,
            line.CashAccountId,
            line.BankAccountId,
            line.SettlementAccountId,
            line.ReferenceNumber,
            cancellationToken);
        var rate = await exchangeRates.ResolveAsync(
            line.CurrencyId,
            documentDate,
            ExchangeRateType.Accounting,
            cancellationToken: cancellationToken);
        var baseCurrency = await GetBaseCurrencyAsync(cancellationToken);
        var amount = rounding.Round(line.Amount, rate.CurrencyDecimalPlaces);
        var baseAmount = rounding.CalculateBaseAmount(
            amount, rate.Rate, rate.CurrencyDecimalPlaces, baseCurrency.DecimalPlaces);
        return new ResolvedPaymentLine(
            Guid.NewGuid(),
            customerId,
            partyName,
            counterpartyAccountId,
            paymentMethod,
            settlement.CashAccountId,
            settlement.BankAccountId,
            settlement.AccountId,
            rate.CurrencyId,
            rate.CurrencyCode,
            rate.CurrencySymbol,
            rate.CurrencyDecimalPlaces,
            amount,
            rate.Rate,
            rate.RateDate,
            rate.RateType,
            rate.Source,
            baseAmount,
            line.ReferenceNumber,
            line.ReferenceDate,
            line.Description);
    }

    private async Task<decimal> GetInvoiceOutstandingBaseAsync(SalesInvoice invoice, CancellationToken cancellationToken)
    {
        var rows = await allocations.ListAsync(
            new Specification<PaymentAllocation>().Where(x =>
                x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice && x.TargetDocumentId == invoice.Id),
            cancellationToken);
        return Math.Max(0m, invoice.BaseTotalAmount - rows.Sum(GetBaseAllocatedAmount));
    }

    private static decimal GetBaseAllocatedAmount(PaymentAllocation allocation)
    {
        if (allocation.BaseAllocatedAmount.HasValue)
            return allocation.BaseAllocatedAmount.Value;
        if (allocation.ExchangeRate.HasValue)
            return Math.Round(allocation.AllocatedAmount * allocation.ExchangeRate.Value, 4, MidpointRounding.AwayFromZero);
        return allocation.AllocatedAmount;
    }

    private static void ValidatePaymentLine(CheckoutPaymentLineRequest line)
    {
        if (!Enum.IsDefined(line.PaymentMethod))
            throw new ConflictException("sales_payment_method_invalid", "طريقة الدفع غير صالحة.");
        if (line.CurrencyId == Guid.Empty)
            throw new ConflictException("sales_payment_currency_required", "عملة سطر الدفع مطلوبة.");
        if (line.Amount <= 0m)
            throw new ConflictException("sales_payment_amount_invalid", "مبلغ سطر الدفع يجب أن يكون أكبر من صفر.");
        if (line.PaymentMethod == ContractPaymentMethod.Cash && !line.CashAccountId.HasValue)
            throw new ConflictException("sales_payment_cash_account_required", "يجب اختيار الصندوق للدفع النقدي.");
        if (line.PaymentMethod is ContractPaymentMethod.Card or ContractPaymentMethod.BankTransfer or ContractPaymentMethod.Cheque && !line.BankAccountId.HasValue)
            throw new ConflictException("sales_payment_bank_account_required", "يجب اختيار الحساب البنكي لطريقة الدفع المحددة.");
        if (line.PaymentMethod == ContractPaymentMethod.Cheque && string.IsNullOrWhiteSpace(line.ReferenceNumber))
            throw new ConflictException("sales_payment_cheque_reference_required", "رقم الشيك مطلوب.");
        if (line.PaymentMethod == ContractPaymentMethod.Other && !line.SettlementAccountId.HasValue)
            throw new ConflictException("sales_payment_settlement_account_required", "يجب تحديد حساب التسوية لطريقة الدفع الأخرى.");
    }

    private async Task<Guid> ResolveAdvanceLiabilityAccountAsync(CancellationToken cancellationToken)
    {
        var profiles = await postingProfiles.ListAsync(
            new Specification<PostingProfile>().Where(x =>
                x.Module == "Accounting" && x.DocumentType == "CustomerAdvanceApplication" && x.IsActive),
            cancellationToken);
        if (profiles.Count != 1)
            throw new ConflictException(
                profiles.Count == 0 ? "customer_advance_posting_profile_missing" : "customer_advance_posting_profile_duplicate",
                "يجب إعداد Posting Profile واحد فعال لعربون العميل.");
        var roleLines = await postingProfileLines.ListAsync(
            new Specification<PostingProfileLine>().Where(x =>
                x.PostingProfileId == profiles[0].Id && x.AccountRole == "CustomerAdvances"),
            cancellationToken);
        if (roleLines.Count != 1)
            throw new ConflictException(
                roleLines.Count == 0 ? "customer_advance_posting_role_missing" : "customer_advance_posting_role_duplicate",
                "يجب تعريف حساب واحد فقط للدور CustomerAdvances في Posting Profile.");
        return roleLines[0].AccountId;
    }

    private async Task<Currency> GetBaseCurrencyAsync(CancellationToken cancellationToken)
    {
        var settings = await accountingSettings.GetByIdAsync(AccountingSettings.SingletonId, cancellationToken)
            ?? throw new ConflictException("accounting_settings_required", "يجب إعداد المحاسبة والعملة الأساسية أولًا.");
        return await currencies.GetByIdAsync(settings.BaseCurrencyId, cancellationToken)
            ?? throw new ConflictException("base_currency_missing", "العملة الأساسية المحددة غير موجودة.");
    }

    private async Task<string> ReserveVoucherNumberAsync(int year, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 100; i++)
        {
            var sequence = await sequenceNumberGenerator.NextAsync($"ReceiptVoucher-{year}", cancellationToken);
            var number = $"RV-{year:0000}-{sequence:000000}";
            if (await receiptVouchers.CountAsync(
                    new Specification<ReceiptVoucher>().Where(x => x.VoucherNumber == number), cancellationToken) == 0)
                return number;
        }
        throw new ConflictException("receipt_voucher_number_duplicate", "تعذر حجز رقم فريد لسند القبض.");
    }

    private Guid GetCurrentUserId()
    {
        if (!Guid.TryParse(currentUser.UserId, out var id) || id == Guid.Empty)
            throw new ForbiddenException();
        return id;
    }

    private sealed record ResolvedPaymentLine(
        Guid LineId,
        Guid? CustomerId,
        string PartyName,
        Guid CounterpartyAccountId,
        DomainPaymentMethod PaymentMethod,
        Guid? CashAccountId,
        Guid? BankAccountId,
        Guid SettlementAccountId,
        Guid CurrencyId,
        string CurrencyCode,
        string? CurrencySymbol,
        byte CurrencyDecimalPlaces,
        decimal Amount,
        decimal ExchangeRate,
        DateOnly ExchangeRateDate,
        ExchangeRateType ExchangeRateType,
        ExchangeRateSource ExchangeRateSource,
        decimal BaseAmount,
        string? ReferenceNumber,
        DateOnly? ReferenceDate,
        string? Description);
}
