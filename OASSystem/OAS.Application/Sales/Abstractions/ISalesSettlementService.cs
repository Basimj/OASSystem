using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Common;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public sealed record SalesPaymentCollectionResult(
    Guid ReceiptVoucherId,
    string ReceiptVoucherNumber,
    IReadOnlyList<Guid> ReceiptVoucherLineIds,
    IReadOnlyList<Guid> PaymentAllocationIds,
    IReadOnlyList<Guid> CustomerAdvanceIds,
    decimal BaseAmount);

public sealed record SalesSettlementReferences(
    IReadOnlyList<Guid> ReceiptVoucherIds,
    IReadOnlyList<Guid> CustomerAdvanceIds);

public interface ISalesSettlementService
{
    Task<decimal> CalculatePaymentBaseAmountAsync(
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        DateOnly documentDate,
        CancellationToken cancellationToken = default);

    Task<SalesPaymentCollectionResult> CreateReceiptForInvoiceAsync(
        SalesInvoice invoice,
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        CancellationToken cancellationToken = default);

    Task<SalesPaymentCollectionResult> CreateAdvanceForOrderAsync(
        CustomerOrder order,
        IReadOnlyList<CheckoutPaymentLineRequest> paymentLines,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ApplyAdvancesToInvoiceAsync(
        CustomerOrder order,
        SalesInvoice invoice,
        CancellationToken cancellationToken = default);

    Task<SalesPaymentSummaryDto> GetPaymentSummaryAsync(
        CustomerOrder order,
        SalesInvoice? invoice,
        decimal paidNowAmount = 0m,
        CancellationToken cancellationToken = default);

    Task<SalesSettlementReferences> GetReferencesAsync(
        CustomerOrder order,
        SalesInvoice? invoice,
        CancellationToken cancellationToken = default);
}
