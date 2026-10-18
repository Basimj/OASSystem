using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Returns.Queries;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Returns.Services;

/// <summary>Credit entitlement is a base-currency right, derived from Posted returns.
/// Historical returns remain in place; this ledger only records USES of that right.</summary>
public sealed class CustomerCreditLedgerService(
    ISender mediator,
    IReadRepository<CustomerCreditApplication,Guid> applications)
{
    public async Task<CustomerCreditBalanceDto> GetAsync(Guid customerId,CancellationToken ct=default)
    {
        var preview=await mediator.Send(new GetCustomerReturnCreditPreviewQuery(customerId),ct);
        var rows=await applications.ListAsync(new Specification<CustomerCreditApplication>()
            .Where(x=>x.CustomerId==customerId),ct);
        var consumed=rows.Where(x=>x.Status==CustomerCreditApplicationStatus.Active)
            .Sum(x=>x.AppliedSourceBaseAmount);
        // Preview is the gross entitlement, independent of any applications.
        var available=Math.Max(0m,preview.TotalHistoricalBaseCredit-consumed);
        return new CustomerCreditBalanceDto(customerId,preview.TotalHistoricalBaseCredit,
            consumed,available,preview.Returns,
            rows.OrderByDescending(x=>x.AppliedAtUtc).Select(Map).ToArray());
    }

    public async Task<(CustomerReturnCreditItemDto Source,decimal Available)> GetAvailableReturnAsync(
        Guid customerId,Guid returnId,CancellationToken ct=default)
    {
        var data=await GetAsync(customerId,ct);
        var source=data.Returns.FirstOrDefault(x=>x.SalesReturnId==returnId)
            ?? throw new ConflictException("customer_credit_return_not_eligible",
                "المرتجع ليس مرحلاً أو لم ينتج رصيدًا دائنًا لهذا العميل.");
        var consumed=data.Applications.Where(x=>x.SalesReturnId==returnId &&
            x.Status==(byte)CustomerCreditApplicationStatus.Active)
            .Sum(x=>x.AppliedSourceBaseAmount);
        return (source,Math.Max(0m,source.EstimatedUnappliedBaseCredit-consumed));
    }

    public static CustomerCreditSettlementDto Map(CustomerCreditApplication x) => new(
        x.Id,x.SalesReturnId,x.CustomerId,(byte)x.SettlementKind,
        x.TargetSalesInvoiceId,x.RefundPaymentVoucherLineId,x.ReversalReceiptVoucherLineId,x.AppliedSourceBaseAmount,
        x.TargetBaseAmount,x.TargetInvoiceCurrencyAmount,x.RefundSettlementBaseAmount,
        x.AppliedAtUtc,(byte)x.Status,Convert.ToBase64String(x.RowVersion));
}
