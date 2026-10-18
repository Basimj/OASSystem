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
    IReadRepository<PaymentAllocation, Guid> allocations,
    IReadRepository<CustomerCreditApplication, Guid>? credits = null) : ISalesInvoiceBalanceService
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
                x.TargetDocumentId == invoice.Id && x.ReversedAtUtc == null),
            cancellationToken);
        if (excludingAllocationId.HasValue)
            allocationRows = allocationRows.Where(x=>x.Id!=excludingAllocationId.Value).ToArray();
        IReadOnlyList<CustomerCreditApplication> creditRows = credits is null
            ? []
            : await credits.ListAsync(new Specification<CustomerCreditApplication>().Where(x =>
                x.TargetSalesInvoiceId == invoice.Id &&
                x.Status == CustomerCreditApplicationStatus.Active), cancellationToken);

        // Original invoice return already posted Cr AR; do NOT count it as a credit
        // application to this SAME invoice. Only cross-invoice applications enter here.
        var returnedBase = Math.Min(invoice.BaseTotalAmount,
            Math.Max(0m,postedReturns.Sum(x=>x.BaseTotalAmount)));
        var returned = Math.Min(invoice.TotalAmount,
            Math.Max(0m,postedReturns.Sum(x=>x.TotalAmount)));
        var netBase = Math.Max(0m,invoice.BaseTotalAmount-returnedBase);
        var net = Math.Max(0m,invoice.TotalAmount-returned);
        var totalAllocatedBase = allocationRows.Sum(x=>TargetBase(x,invoice.BaseCurrencyDecimalPlacesSnapshot))+
            creditRows.Sum(x=>x.TargetBaseAmount??0m);
        var totalAllocated = allocationRows.Sum(x=>TargetAmount(x,invoice))+
            creditRows.Sum(x=>x.TargetInvoiceCurrencyAmount??0m);
        // Do not hide over-allocation by clamping: outstanding is capped at zero for
        // display, while callers can still detect the raw allocated amount.
        var allocatedBase=Math.Max(0m,totalAllocatedBase);
        var allocated=Math.Max(0m,totalAllocated);
        return new SalesInvoiceBalance(invoice.TotalAmount,returned,net,
            allocated,Math.Max(0m,net-allocated),invoice.BaseTotalAmount,
            returnedBase,netBase,allocatedBase,Math.Max(0m,netBase-allocatedBase));
    }
    private static decimal TargetAmount(PaymentAllocation x,SalesInvoice invoice)
    {
        if (x.TargetDocumentAmount is >0m)return x.TargetDocumentAmount.Value;
        if (x.CurrencyId==invoice.CurrencyId)return x.AllocatedAmount;
        var targetBase=TargetBase(x,invoice.BaseCurrencyDecimalPlacesSnapshot);
        return invoice.ExchangeRate<=0m?0m:Math.Round(targetBase/invoice.ExchangeRate,
            invoice.CurrencyDecimalPlacesSnapshot,MidpointRounding.AwayFromZero);
    }
    private static decimal TargetBase(PaymentAllocation x,byte decimals)
    {
        if(x.TargetBaseAllocatedAmount.HasValue)return x.TargetBaseAllocatedAmount.Value;
        if(x.BaseAllocatedAmount.HasValue)return x.BaseAllocatedAmount.Value;
        return x.ExchangeRate.HasValue?Math.Round(x.AllocatedAmount*x.ExchangeRate.Value,
            decimals,MidpointRounding.AwayFromZero):x.AllocatedAmount;
    }
}
