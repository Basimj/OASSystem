using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Returns.Services;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;
using DomainSalesInvoiceStatus = OAS.Domain.Sales.Enums.SalesInvoiceStatus;
using DomainSalesReturnStatus = OAS.Domain.Sales.Enums.SalesReturnStatus;

namespace OAS.Application.Sales.Returns.Queries;

public sealed record GetCustomerReturnCreditPreviewQuery(Guid CustomerId)
    : IQuery<CustomerReturnCreditPreviewDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.View];
}

public sealed class GetCustomerReturnCreditPreviewQueryHandler(
    IReadRepository<SalesReturn, Guid> salesReturns,
    IReadRepository<SalesInvoice, Guid> invoices,
    IReadRepository<PaymentAllocation, Guid> allocations)
    : IRequestHandler<GetCustomerReturnCreditPreviewQuery, CustomerReturnCreditPreviewDto>
{
    public async Task<CustomerReturnCreditPreviewDto> Handle(
        GetCustomerReturnCreditPreviewQuery request,
        CancellationToken ct)
    {
        if (request.CustomerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required.");

        var posted = await salesReturns.ListAsync(
            new Specification<SalesReturn>().Where(x =>
                x.CustomerId == request.CustomerId && x.Status == DomainSalesReturnStatus.Posted), ct);
        if (posted.Count == 0)
            return new CustomerReturnCreditPreviewDto(request.CustomerId, 0m, []);

        var invoiceIds = posted.Select(x => x.SalesInvoiceId).Distinct().ToArray();
        var invoiceRows = await invoices.ListAsync(
            new Specification<SalesInvoice>().Where(x =>
                invoiceIds.Contains(x.Id) && x.CustomerId == request.CustomerId &&
                x.Status == DomainSalesInvoiceStatus.Posted), ct);
        var invoiceMap = invoiceRows.ToDictionary(x => x.Id);

        var payments = await allocations.ListAsync(
            new Specification<PaymentAllocation>().Where(x =>
                x.TargetDocumentType == AllocationTargetDocumentType.SalesInvoice &&
                invoiceIds.Contains(x.TargetDocumentId) && x.ReversedAtUtc == null), ct);

        var result = new List<CustomerReturnCreditItemDto>(posted.Count);
        foreach (var invoiceGroup in posted.GroupBy(x => x.SalesInvoiceId))
        {
            if (!invoiceMap.TryGetValue(invoiceGroup.Key, out var invoice))
                throw new InvalidOperationException("A posted customer return points to a missing or non-posted customer invoice.");

            decimal allocatedInvoiceCurrency = payments
                .Where(x => x.TargetDocumentId == invoice.Id)
                .Sum(x => TargetInvoiceAmount(x, invoice));

            var sources = invoiceGroup.Select(x => new CustomerReturnCreditCalculator.PostedReturn(
                x.Id, x.ReturnCode, x.ReturnDate, x.PostedAtUtc,
                x.TotalAmount, x.BaseTotalAmount)).ToArray();

            result.AddRange(CustomerReturnCreditCalculator.Calculate(
                request.CustomerId, invoice.Id, invoice.InvoiceCode,
                invoice.CurrencyId, invoice.CurrencyCodeSnapshot,
                invoice.BaseCurrencyId, invoice.BaseCurrencyCodeSnapshot,
                invoice.TotalAmount, allocatedInvoiceCurrency,
                invoice.BaseCurrencyDecimalPlacesSnapshot, sources));
        }

        // All amounts in the aggregate are expressed in the same system base
        // currency. The individual items retain their original invoice currency.
        return new CustomerReturnCreditPreviewDto(
            request.CustomerId,
            result.Sum(x => x.EstimatedUnappliedBaseCredit),
            result.OrderBy(x => x.ReturnDate).ThenBy(x => x.SalesReturnId).ToArray());
    }

    private static decimal TargetInvoiceAmount(PaymentAllocation allocation, SalesInvoice invoice)
    {
        if (allocation.TargetDocumentAmount.HasValue)
            return allocation.TargetDocumentAmount.Value;
        if (allocation.CurrencyId == invoice.CurrencyId)
            return allocation.AllocatedAmount;
        var targetBase = allocation.TargetBaseAllocatedAmount ?? allocation.BaseAllocatedAmount
            ?? (allocation.ExchangeRate.HasValue ? allocation.AllocatedAmount * allocation.ExchangeRate.Value : 0m);
        if (invoice.ExchangeRate <= 0m)
            throw new InvalidOperationException("The historical invoice FX rate is invalid.");
        return Math.Round(targetBase / invoice.ExchangeRate,
            invoice.CurrencyDecimalPlacesSnapshot, MidpointRounding.AwayFromZero);
    }
}
