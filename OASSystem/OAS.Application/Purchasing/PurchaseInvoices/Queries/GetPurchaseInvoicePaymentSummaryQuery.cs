using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;
using ContractExchangeRateSource = OAS.Contracts.Accounting.Enums.ExchangeRateSource;
using ContractExchangeRateType = OAS.Contracts.Accounting.Enums.ExchangeRateType;
using DomainExchangeRateSource = OAS.Domain.Accounting.Enums.ExchangeRateSource;
using DomainExchangeRateType = OAS.Domain.Accounting.Enums.ExchangeRateType;

namespace OAS.Application.Purchasing.PurchaseInvoices.Queries;

public sealed record GetPurchaseInvoicePaymentSummaryQuery(Guid Id)
    : IQuery<PurchaseInvoicePaymentSummaryDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Invoices.View];
}

public sealed class GetPurchaseInvoicePaymentSummaryQueryHandler(
    IReadRepository<PurchaseInvoice, Guid> invoices,
    IReadRepository<PaymentAllocation, Guid> paymentAllocations,
    IReadRepository<PaymentVoucherLine, Guid> paymentVoucherLines,
    IReadRepository<JournalEntry, Guid> journals,
    IPurchaseInvoiceBalanceService balances)
    : IRequestHandler<GetPurchaseInvoicePaymentSummaryQuery, PurchaseInvoicePaymentSummaryDto>
{
    private const string FxSourceModule = "Accounting";
    private const string FxSourceDocumentType = "SupplierSettlementFx";

    public async Task<PurchaseInvoicePaymentSummaryDto> Handle(GetPurchaseInvoicePaymentSummaryQuery request, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), request.Id);

        if (invoice.Status != PurchaseInvoiceStatus.Posted)
        {
            return new PurchaseInvoicePaymentSummaryDto(
                invoice.Id,
                invoice.TotalAmount,
                0m,
                invoice.TotalAmount,
                "NotPosted",
                0m,
                0m,
                invoice.BaseTotalAmount,
                0m,
                0m,
                invoice.BaseTotalAmount,
                0m,
                Array.Empty<PurchaseInvoicePaymentSettlementDto>(),
                invoice.BaseCurrencyCodeSnapshot);
        }

        var balance = await balances.GetAsync(invoice, null, ct);
        var allocations = await paymentAllocations.ListAsync(
            new Specification<PaymentAllocation>().Where(x =>
                x.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice &&
                x.TargetDocumentId == invoice.Id &&
                x.ReversedAtUtc == null),
            ct);

        var settlements = new List<PurchaseInvoicePaymentSettlementDto>(allocations.Count);
        foreach (var allocation in allocations.OrderBy(x => x.AllocatedAtUtc))
        {
            PaymentVoucherLine? line = null;
            if (allocation.PaymentVoucherLineId is Guid paymentLineId)
                line = await paymentVoucherLines.GetByIdAsync(paymentLineId, ct);

            var fxJournals = await journals.ListAsync(
                new Specification<JournalEntry>()
                    .Where(x =>
                        x.SourceModule == FxSourceModule &&
                        x.SourceDocumentType == FxSourceDocumentType &&
                        x.SourceDocumentId == allocation.Id)
                    .ApplyPaging(0, 1),
                ct);

            var sourceRate = allocation.ExchangeRate ?? line?.ExchangeRate;
            var sourceBase = GetSourceBaseAmount(allocation, sourceRate);
            var targetBase = GetTargetBaseAmount(allocation, sourceBase);
            var targetDocument = GetTargetDocumentAmount(allocation, invoice, targetBase);
            var fxDifference = Round(sourceBase - targetBase);

            settlements.Add(new PurchaseInvoicePaymentSettlementDto(
                allocation.Id,
                line?.PaymentVoucherId,
                allocation.PaymentVoucherLineId,
                line?.CurrencyCodeSnapshot ?? allocation.CurrencyCodeSnapshot,
                allocation.AllocatedAmount,
                sourceRate,
                line?.ExchangeRateDate,
                MapRateType(line?.ExchangeRateType),
                MapRateSource(line?.ExchangeRateSource),
                sourceBase,
                targetDocument,
                targetBase,
                fxDifference,
                fxJournals.FirstOrDefault()?.Id,
                allocation.AllocatedAtUtc));
        }

        var status = balance.OutstandingAmount <= 0m && balance.OutstandingBaseAmount <= 0m
            ? "Paid"
            : balance.SettledAmount > 0m
                ? "PartiallyPaid"
                : "Unpaid";

        return new PurchaseInvoicePaymentSummaryDto(
            invoice.Id,
            invoice.TotalAmount,
            balance.PaidAmount,
            balance.OutstandingAmount,
            status,
            balance.AppliedCreditInvoiceCurrencyAmount,
            balance.SettledAmount,
            invoice.BaseTotalAmount,
            balance.AppliedPaymentBaseAmount,
            balance.AppliedCreditBaseAmount,
            balance.OutstandingBaseAmount,
            balance.RealizedExchangeDifferenceBase,
            settlements,
            invoice.BaseCurrencyCodeSnapshot);
    }

    private static decimal GetSourceBaseAmount(PaymentAllocation allocation, decimal? sourceRate)
    {
        if (allocation.BaseAllocatedAmount is > 0m)
            return allocation.BaseAllocatedAmount.Value;
        if (sourceRate is > 0m)
            return Round(allocation.AllocatedAmount * sourceRate.Value);
        return allocation.AllocatedAmount;
    }

    private static decimal GetTargetBaseAmount(PaymentAllocation allocation, decimal sourceBase)
        => allocation.TargetBaseAllocatedAmount is > 0m
            ? allocation.TargetBaseAllocatedAmount.Value
            : sourceBase;

    private static decimal GetTargetDocumentAmount(PaymentAllocation allocation, PurchaseInvoice invoice, decimal targetBase)
    {
        if (allocation.TargetDocumentAmount is > 0m)
            return allocation.TargetDocumentAmount.Value;
        if (allocation.CurrencyId == invoice.CurrencyId)
            return allocation.AllocatedAmount;
        return invoice.ExchangeRate > 0m ? Round(targetBase / invoice.ExchangeRate) : 0m;
    }

    private static ContractExchangeRateType? MapRateType(DomainExchangeRateType? value)
        => value.HasValue ? (ContractExchangeRateType)(byte)value.Value : null;

    private static ContractExchangeRateSource? MapRateSource(DomainExchangeRateSource? value)
        => value.HasValue ? (ContractExchangeRateSource)(byte)value.Value : null;

    private static decimal Round(decimal value)
        => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
