using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class SalesInvoiceBalanceService(
    IReadRepository<SalesReturn, Guid> returns,
    IReadRepository<PaymentAllocation, Guid> allocations) : ISalesInvoiceBalanceService
{
    public async Task<SalesInvoiceBalance> GetAsync(
        SalesInvoice invoice,
        Guid? excludingAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var postedReturns = await returns.ListAsync(
            new Specification<SalesReturn>().Where(x =>
                x.SalesInvoiceId == invoice.Id && x.Status == SalesReturnStatus.Posted),
            cancellationToken);

        var allocationRows = await allocations.ListAsync(
            new Specification<PaymentAllocation>().Where(x =>
                x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice &&
                x.TargetDocumentId == invoice.Id),
            cancellationToken);

        if (excludingAllocationId.HasValue)
            allocationRows = allocationRows.Where(x => x.Id != excludingAllocationId.Value).ToArray();

        var returnedBase = Math.Min(
            invoice.BaseTotalAmount,
            Math.Max(0m, postedReturns.Sum(x => x.BaseTotalAmount)));
        var returned = Math.Min(
            invoice.TotalAmount,
            Math.Max(0m, postedReturns.Sum(x => x.TotalAmount)));

        var netBase = Math.Max(0m, invoice.BaseTotalAmount - returnedBase);
        var net = Math.Max(0m, invoice.TotalAmount - returned);
        var allocatedBase = Math.Max(0m, allocationRows.Sum(x =>
            GetTargetBaseAllocatedAmount(x, invoice.BaseCurrencyDecimalPlacesSnapshot)));

        // Preserve exact transaction-currency allocations when payment and invoice use the
        // same currency. Only cross-currency allocations need to be translated back using
        // the invoice's immutable exchange-rate snapshot. Otherwise a later collection rate
        // could make a 300 USD payment appear as (for example) 312 USD against the invoice.
        var sameCurrencyRows = allocationRows.Where(x => x.CurrencyId == invoice.CurrencyId).ToArray();
        var sameCurrencyAllocated = Math.Max(0m, sameCurrencyRows.Sum(x => x.AllocatedAmount));
        var sameCurrencyTargetBase = Math.Max(0m, sameCurrencyRows.Sum(x =>
            GetTargetBaseAllocatedAmount(x, invoice.BaseCurrencyDecimalPlacesSnapshot)));
        var crossCurrencyTargetBase = Math.Max(0m, allocatedBase - sameCurrencyTargetBase);
        var crossCurrencyInInvoiceCurrency = invoice.ExchangeRate <= 0m
            ? 0m
            : Math.Round(
                crossCurrencyTargetBase / invoice.ExchangeRate,
                invoice.CurrencyDecimalPlacesSnapshot,
                MidpointRounding.AwayFromZero);
        var allocated = Math.Min(net, Math.Max(0m, sameCurrencyAllocated + crossCurrencyInInvoiceCurrency));

        return new SalesInvoiceBalance(
            invoice.TotalAmount,
            returned,
            net,
            allocated,
            Math.Max(0m, net - allocated),
            invoice.BaseTotalAmount,
            returnedBase,
            netBase,
            allocatedBase,
            Math.Max(0m, netBase - allocatedBase));
    }

    private static decimal GetTargetBaseAllocatedAmount(PaymentAllocation allocation, byte baseCurrencyDecimalPlaces)
    {
        if (allocation.TargetBaseAllocatedAmount.HasValue)
            return allocation.TargetBaseAllocatedAmount.Value;
        if (allocation.BaseAllocatedAmount.HasValue)
            return allocation.BaseAllocatedAmount.Value;
        if (allocation.ExchangeRate.HasValue)
            return Math.Round(
                allocation.AllocatedAmount * allocation.ExchangeRate.Value,
                baseCurrencyDecimalPlaces,
                MidpointRounding.AwayFromZero);
        return allocation.AllocatedAmount;
    }
}
