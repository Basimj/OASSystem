using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Services;

public sealed class SalesCreditExposureService(
    IReadRepository<SalesInvoice, Guid> invoices,
    IReadRepository<PaymentAllocation, Guid> allocations) : ISalesCreditExposureService
{
    public async Task<SalesCreditAssessment> EvaluateAsync(
        Customer customer,
        SalesInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        if (invoice.PaymentTermType != SalesPaymentTermType.Credit)
        {
            return new SalesCreditAssessment(
                true,
                null,
                null,
                0m,
                invoice.BaseTotalAmount,
                customer.CreditLimit,
                invoice.BaseTotalAmount);
        }

        if (!customer.IsCreditAllowed)
        {
            return new SalesCreditAssessment(
                false,
                SalesErrorCodes.CreditNotAllowed,
                "البيع الآجل غير مسموح لهذا العميل.",
                0m,
                invoice.BaseTotalAmount,
                customer.CreditLimit,
                invoice.BaseTotalAmount);
        }

        // Customer.CreditLimit has no currency field in the current OAS model, so Sales treats
        // it as a base-currency limit. Exposure is therefore calculated from base snapshots.
        var before = await CalculateExposureBeforeCurrentAsync(customer.Id, invoice.Id, cancellationToken);
        var newExposure = before + invoice.BaseTotalAmount;

        if (newExposure > customer.CreditLimit)
        {
            return new SalesCreditAssessment(
                false,
                SalesErrorCodes.CreditLimitExceeded,
                "تتجاوز الفاتورة الحد الائتماني المسموح للعميل.",
                before,
                invoice.BaseTotalAmount,
                customer.CreditLimit,
                newExposure);
        }

        return new SalesCreditAssessment(
            true,
            null,
            null,
            before,
            invoice.BaseTotalAmount,
            customer.CreditLimit,
            newExposure);
    }

    public async Task ValidateAsync(Customer customer, SalesInvoice invoice, CancellationToken cancellationToken = default)
    {
        var assessment = await EvaluateAsync(customer, invoice, cancellationToken);
        if (!assessment.IsAllowed)
            throw new ConflictException(assessment.ErrorCode!, assessment.Message!);
    }

    public async Task<decimal> CalculateExposureBeforeCurrentAsync(
        Guid customerId,
        Guid? currentInvoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoiceSpec = new Specification<SalesInvoice>()
            .Where(x => x.CustomerId == customerId && x.Id != currentInvoiceId &&
                        (x.Status == SalesInvoiceStatus.Posted ||
                         (x.Status == SalesInvoiceStatus.Confirmed && x.PaymentTermType == SalesPaymentTermType.Credit)));
        var customerInvoices = await invoices.ListAsync(invoiceSpec, cancellationToken);

        var posted = customerInvoices.Where(x => x.Status == SalesInvoiceStatus.Posted).ToList();
        var confirmedCredit = customerInvoices
            .Where(x => x.Status == SalesInvoiceStatus.Confirmed && x.PaymentTermType == SalesPaymentTermType.Credit)
            .Sum(x => x.BaseTotalAmount);

        decimal postedOutstanding = 0m;
        foreach (var invoice in posted)
        {
            var allocationSpec = new Specification<PaymentAllocation>()
                .Where(x => x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice && x.TargetDocumentId == invoice.Id);
            var invoiceAllocations = await allocations.ListAsync(allocationSpec, cancellationToken);
            var paidBase = invoiceAllocations.Sum(GetBaseAllocatedAmount);
            postedOutstanding += Math.Max(0m, invoice.BaseTotalAmount - paidBase);
        }

        return postedOutstanding + confirmedCredit;
    }

    private static decimal GetBaseAllocatedAmount(PaymentAllocation allocation)
    {
        if (allocation.BaseAllocatedAmount.HasValue)
            return allocation.BaseAllocatedAmount.Value;
        if (allocation.ExchangeRate.HasValue)
            return Math.Round(allocation.AllocatedAmount * allocation.ExchangeRate.Value, 4, MidpointRounding.AwayFromZero);
        return allocation.AllocatedAmount;
    }
}
