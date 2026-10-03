using OAS.Contracts.Sales.Enums;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Abstractions;

public sealed record SalesImmediateSettlementResult(
    Guid ReceiptVoucherId,
    string ReceiptVoucherNumber,
    Guid ReceiptJournalEntryId,
    Guid PaymentAllocationId);

public interface ISalesImmediateSettlementService
{
    Task<SalesImmediateSettlementResult> SettleAsync(
        SalesInvoice invoice,
        SalesImmediatePaymentMethod paymentMethod,
        Guid? cashAccountId,
        Guid? bankAccountId,
        Guid postedBy,
        DateTime postedAtUtc,
        CancellationToken cancellationToken = default);
}
