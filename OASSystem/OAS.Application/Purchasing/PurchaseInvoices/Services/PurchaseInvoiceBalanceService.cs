using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.PurchaseInvoices.Services;

public sealed class PurchaseInvoiceBalanceService(
    IReadRepository<PurchaseInvoice, Guid> invoices,
    IReadRepository<PaymentAllocation, Guid> paymentAllocations,
    IReadRepository<SupplierCreditApplication, Guid> creditApplications)
    : IPurchaseInvoiceBalanceService
{
    public async Task<PurchaseInvoiceBalanceSnapshot> GetAsync(
        Guid purchaseInvoiceId,
        Guid? excludingPaymentAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        var invoice = await invoices.GetByIdAsync(purchaseInvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), purchaseInvoiceId);
        return await GetAsync(invoice, excludingPaymentAllocationId, cancellationToken);
    }

    public async Task<PurchaseInvoiceBalanceSnapshot> GetAsync(
        PurchaseInvoice invoice,
        Guid? excludingPaymentAllocationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.Status != PurchaseInvoiceStatus.Posted)
        {
            return new PurchaseInvoiceBalanceSnapshot(
                invoice.Id,
                invoice.TotalAmount,
                invoice.BaseTotalAmount,
                0m,
                0m,
                0m,
                invoice.BaseTotalAmount,
                invoice.TotalAmount,
                0m,
                0m);
        }

        var payments = await paymentAllocations.ListAsync(
            new Specification<PaymentAllocation>().Where(x =>
                x.TargetDocumentType == AllocationTargetDocumentType.PurchaseInvoice &&
                x.TargetDocumentId == invoice.Id),
            cancellationToken);

        if (excludingPaymentAllocationId.HasValue)
            payments = payments.Where(x => x.Id != excludingPaymentAllocationId.Value).ToArray();

        var credits = await creditApplications.ListAsync(
            new Specification<SupplierCreditApplication>().Where(x =>
                x.PurchaseInvoiceId == invoice.Id &&
                x.Status == SupplierCreditApplicationStatus.Active),
            cancellationToken);

        var paymentBase = Round(payments.Sum(GetPaymentTargetBaseAmount));
        var creditBase = Round(credits.Sum(x => x.AppliedTargetBaseAmount));
        var creditDocumentAmount = Round(credits.Sum(x => x.AppliedInvoiceCurrencyAmount));
        var outstandingBase = Math.Max(0m, Round(invoice.BaseTotalAmount - paymentBase - creditBase));

        var outstanding = invoice.ExchangeRate > 0m
            ? Math.Min(invoice.TotalAmount, Round(outstandingBase / invoice.ExchangeRate))
            : Math.Max(0m, Round(invoice.TotalAmount - creditDocumentAmount));

        var settled = Math.Max(0m, Round(invoice.TotalAmount - outstanding));
        var paid = Math.Max(0m, Round(settled - Math.Min(settled, creditDocumentAmount)));

        return new PurchaseInvoiceBalanceSnapshot(
            invoice.Id,
            invoice.TotalAmount,
            invoice.BaseTotalAmount,
            paymentBase,
            creditBase,
            creditDocumentAmount,
            outstandingBase,
            outstanding,
            paid,
            settled);
    }

    private static decimal GetPaymentTargetBaseAmount(PaymentAllocation allocation)
    {
        if (allocation.TargetBaseAllocatedAmount.HasValue)
            return allocation.TargetBaseAllocatedAmount.Value;
        if (allocation.BaseAllocatedAmount.HasValue)
            return allocation.BaseAllocatedAmount.Value;
        if (allocation.ExchangeRate.HasValue)
            return Round(allocation.AllocatedAmount * allocation.ExchangeRate.Value);
        return allocation.AllocatedAmount;
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
