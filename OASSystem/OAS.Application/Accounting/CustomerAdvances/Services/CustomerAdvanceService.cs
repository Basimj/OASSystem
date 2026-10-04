using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Accounting.CustomerAdvances;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Accounting.CustomerAdvances.Services;

public sealed class CustomerAdvanceService(
    IRepository<CustomerAdvance, Guid> advances,
    IRepository<CustomerAdvanceApplication, Guid> applications,
    IRepository<PaymentAllocation, Guid> allocations,
    IReadRepository<ReceiptVoucherLine, Guid> receiptLines,
    IReadRepository<ReceiptVoucher, Guid> receipts,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<PostingProfile, Guid> postingProfiles,
    IReadRepository<PostingProfileLine, Guid> postingProfileLines,
    ICustomerOrderAggregateRepository orders,
    IReadRepository<SalesInvoice, Guid> invoices,
    ISequenceNumberGenerator sequences,
    ICurrencyRoundingService rounding,
    IEnumerable<IPaymentAllocationTargetValidator> targetValidators,
    ICustomerAdvanceAccountingPostingService posting,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICustomerAdvanceService
{
    public async Task<CustomerAdvanceDto> CreateAsync(CreateCustomerAdvanceRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);
        var order = await orders.GetAggregateAsync(request.CustomerOrderId, false, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOrder), request.CustomerOrderId);
        if (order.CustomerId != customer.Id)
            throw new ConflictException("customer_advance_order_customer_mismatch", "طلب العميل لا يعود إلى العميل المحدد للعربون.");

        var line = await receiptLines.GetByIdAsync(request.ReceiptVoucherLineId, cancellationToken)
            ?? throw new NotFoundException(nameof(ReceiptVoucherLine), request.ReceiptVoucherLineId);
        var receipt = await receipts.GetByIdAsync(line.ReceiptVoucherId, cancellationToken)
            ?? throw new NotFoundException(nameof(ReceiptVoucher), line.ReceiptVoucherId);
        if (receipt.Status != ReceiptVoucherStatus.Posted)
            throw new ConflictException("customer_advance_receipt_not_posted", "يجب ترحيل سند القبض قبل إنشاء العربون.");
        if (line.CustomerId != customer.Id)
            throw new ConflictException("customer_advance_receipt_customer_mismatch", "سطر سند القبض لا يعود إلى العميل المحدد.");
        if (line.PartyType != SettlementPartyType.Customer)
            throw new ConflictException("customer_advance_receipt_party_invalid", "يجب أن يكون مصدر العربون سطر قبض لطرف من نوع عميل.");

        var advanceLiabilityAccountId = await ResolveAdvanceLiabilityAccountAsync(cancellationToken);
        if (line.CounterpartyAccountId != advanceLiabilityAccountId)
            throw new ConflictException(
                "customer_advance_receipt_account_invalid",
                "سطر سند القبض لم يُرحّل إلى حساب التزامات عربون العملاء المحدد في Posting Profile.");

        if (!line.CurrencyId.HasValue || string.IsNullOrWhiteSpace(line.CurrencyCodeSnapshot) ||
            !line.CurrencyDecimalPlacesSnapshot.HasValue || !line.ExchangeRate.HasValue || !line.ExchangeRateDate.HasValue ||
            !line.ExchangeRateType.HasValue || !line.ExchangeRateSource.HasValue || !line.BaseAmount.HasValue)
            throw new ConflictException("customer_advance_receipt_snapshot_missing", "سطر سند القبض لا يحتوي snapshots العملة اللازمة لإنشاء العربون.");
        if (!receipt.BaseCurrencyId.HasValue || string.IsNullOrWhiteSpace(receipt.BaseCurrencyCodeSnapshot) || !receipt.BaseCurrencyDecimalPlacesSnapshot.HasValue)
            throw new ConflictException("customer_advance_base_currency_missing", "سند القبض لا يحتوي snapshot العملة الأساسية.");

        var duplicateSpec = new Specification<CustomerAdvance>().Where(x => x.ReceiptVoucherLineId == line.Id);
        if (await advances.CountAsync(duplicateSpec, cancellationToken) > 0)
            throw new ConflictException("customer_advance_receipt_already_used", "تم إنشاء عربون مسبقًا من سطر سند القبض هذا.");

        var receivedAt = receipt.PostedAtUtc ?? timeProvider.GetUtcNow().UtcDateTime;
        var sequence = await sequences.NextAsync($"CustomerAdvance-{receivedAt.Year}", cancellationToken);
        var advance = CustomerAdvance.Create(
            Guid.NewGuid(),
            $"ADV-{receivedAt.Year:0000}-{sequence:000000}",
            customer.Id,
            order.Id,
            line.Id,
            line.CurrencyId.Value,
            line.CurrencyCodeSnapshot!,
            line.CurrencySymbolSnapshot,
            line.CurrencyDecimalPlacesSnapshot.Value,
            receipt.BaseCurrencyId.Value,
            receipt.BaseCurrencyCodeSnapshot!,
            receipt.BaseCurrencyDecimalPlacesSnapshot.Value,
            line.Amount,
            line.ExchangeRate.Value,
            line.ExchangeRateDate.Value,
            line.ExchangeRateType.Value,
            line.ExchangeRateSource.Value,
            line.BaseAmount.Value,
            receivedAt);

        await advances.AddAsync(advance, cancellationToken);
        return Map(advance);
    }

    public async Task<CustomerAdvanceApplicationDto> ApplyAsync(
        Guid customerAdvanceId,
        ApplyCustomerAdvanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var advance = await advances.GetForUpdateAsync(customerAdvanceId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerAdvance), customerAdvanceId);
        SalesConcurrency.Ensure(request.RowVersion, advance.RowVersion, "عربون العميل");

        var invoice = await invoices.GetByIdAsync(request.SalesInvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesInvoice), request.SalesInvoiceId);
        if (invoice.CustomerId != advance.CustomerId)
            throw new ConflictException("customer_advance_invoice_customer_mismatch", "لا يمكن تطبيق عربون عميل على فاتورة عميل آخر.");
        if (request.Amount <= 0m || request.Amount > advance.AvailableAmount)
            throw new ConflictException("customer_advance_amount_invalid", "مبلغ تطبيق العربون يجب أن يكون موجبًا ولا يتجاوز الرصيد المتاح.");

        var targetValidator = targetValidators.SingleOrDefault(x => x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice)
            ?? throw new ConflictException("customer_advance_sales_validator_missing", "خدمة التحقق من تخصيص دفعات فواتير المبيعات غير مسجلة.");

        var sourceBaseAmount = rounding.CalculateBaseAmount(
            request.Amount,
            advance.ExchangeRate,
            advance.CurrencyDecimalPlacesSnapshot,
            advance.BaseCurrencyDecimalPlacesSnapshot);
        var target = await targetValidator.ValidateAsync(
            invoice.Id,
            advance.CurrencyId,
            request.Amount,
            sourceBaseAmount,
            null,
            cancellationToken);

        var applicationId = Guid.NewGuid();
        var appliedAt = timeProvider.GetUtcNow().UtcDateTime;
        var postingResult = await posting.PostApplicationAsync(
            applicationId,
            advance,
            invoice,
            request.Amount,
            sourceBaseAmount,
            target.TargetBaseAllocatedAmount,
            currentUser.UserId,
            appliedAt,
            cancellationToken);

        var application = CustomerAdvanceApplication.Create(
            applicationId,
            advance.Id,
            invoice.Id,
            request.Amount,
            sourceBaseAmount,
            postingResult.TargetBaseAmount,
            postingResult.JournalEntryId,
            appliedAt,
            currentUser.UserId);

        advance.Apply(request.Amount, sourceBaseAmount);
        advances.Update(advance);
        await applications.AddAsync(application, cancellationToken);

        var allocation = PaymentAllocation.CreateCustomerAdvanceAllocation(
            Guid.NewGuid(),
            application.Id,
            AllocationTargetDocumentType.SalesInvoice,
            invoice.Id,
            advance.CurrencyId,
            advance.CurrencyCodeSnapshot,
            request.Amount,
            advance.ExchangeRate,
            sourceBaseAmount,
            postingResult.TargetBaseAmount,
            appliedAt);
        await allocations.AddAsync(allocation, cancellationToken);

        return Map(application);
    }

    public async Task<CustomerAdvanceDto> GetAsync(Guid customerAdvanceId, CancellationToken cancellationToken = default)
    {
        var advance = await advances.GetByIdAsync(customerAdvanceId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerAdvance), customerAdvanceId);
        return Map(advance);
    }


    private async Task<Guid> ResolveAdvanceLiabilityAccountAsync(CancellationToken cancellationToken)
    {
        var profileSpec = new Specification<PostingProfile>()
            .Where(x => x.Module == "Accounting" && x.DocumentType == "CustomerAdvanceApplication" && x.IsActive);
        var activeProfiles = await postingProfiles.ListAsync(profileSpec, cancellationToken);
        if (activeProfiles.Count == 0)
            throw new ConflictException("customer_advance_posting_profile_missing", "لم يتم إعداد Posting Profile فعال لعربون العميل.");
        if (activeProfiles.Count > 1)
            throw new ConflictException("customer_advance_posting_profile_duplicate", "يوجد أكثر من Posting Profile فعال لعربون العميل.");

        var profileId = activeProfiles[0].Id;
        var linesSpec = new Specification<PostingProfileLine>()
            .Where(x => x.PostingProfileId == profileId && x.AccountRole == "CustomerAdvances");
        var roleLines = await postingProfileLines.ListAsync(linesSpec, cancellationToken);
        if (roleLines.Count != 1)
            throw new ConflictException(
                roleLines.Count == 0 ? "customer_advance_posting_role_missing" : "customer_advance_posting_role_duplicate",
                "يجب تعريف حساب واحد فقط للدور CustomerAdvances في Posting Profile.");
        return roleLines[0].AccountId;
    }

    private static CustomerAdvanceDto Map(CustomerAdvance x) => new(
        x.Id,
        x.AdvanceNumber,
        x.CustomerId,
        x.CustomerOrderId,
        x.ReceiptVoucherLineId,
        x.CurrencyId,
        x.CurrencyCodeSnapshot,
        x.CurrencySymbolSnapshot,
        x.CurrencyDecimalPlacesSnapshot,
        x.BaseCurrencyId,
        x.BaseCurrencyCodeSnapshot,
        x.BaseCurrencyDecimalPlacesSnapshot,
        x.Amount,
        x.ExchangeRate,
        x.ExchangeRateDate,
        (OAS.Contracts.Accounting.Enums.ExchangeRateType)(byte)x.ExchangeRateType,
        (OAS.Contracts.Accounting.Enums.ExchangeRateSource)(byte)x.ExchangeRateSource,
        x.BaseAmount,
        x.AppliedAmount,
        x.BaseAppliedAmount,
        x.AvailableAmount,
        x.BaseAvailableAmount,
        (OAS.Contracts.Accounting.Enums.CustomerAdvanceStatus)(byte)x.Status,
        x.ReceivedAtUtc,
        x.IsActive,
        Convert.ToBase64String(x.RowVersion),
        x.CreatedAtUtc,
        x.CreatedBy,
        x.LastModifiedAtUtc,
        x.LastModifiedBy);

    private static CustomerAdvanceApplicationDto Map(CustomerAdvanceApplication x) => new(
        x.Id,
        x.CustomerAdvanceId,
        x.SalesInvoiceId,
        x.Amount,
        x.BaseAmount,
        x.TargetBaseAmount,
        x.JournalEntryId,
        x.AppliedAtUtc,
        x.AppliedBy,
        Convert.ToBase64String(x.RowVersion),
        x.CreatedAtUtc,
        x.CreatedBy,
        x.LastModifiedAtUtc,
        x.LastModifiedBy);
}
