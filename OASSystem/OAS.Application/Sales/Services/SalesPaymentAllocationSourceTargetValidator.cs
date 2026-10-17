using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Services;

public sealed class SalesPaymentAllocationSourceTargetValidator(
    IReadRepository<SalesInvoice, Guid> invoices)
    : IPaymentAllocationSourceTargetValidator
{
    public AllocationTargetDocumentType TargetDocumentType =>
        AllocationTargetDocumentType.SalesInvoice;

    public async Task ValidateSourceAsync(
        Guid targetDocumentId,
        PaymentAllocationSourceContext source,
        CancellationToken cancellationToken = default)
    {
        var invoice = await invoices.GetByIdAsync(targetDocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesInvoice), targetDocumentId);

        if (source.SourceType != PaymentSourceType.ReceiptVoucher ||
            source.PartyType != SettlementPartyType.Customer ||
            source.CustomerId is not Guid customerId ||
            customerId == Guid.Empty)
        {
            throw new ConflictException(
                "sales_invoice_receipt_source_invalid",
                "يمكن تخصيص فاتورة المبيعات من سطر سند قبض خاص بالعميل فقط.");
        }

        if (invoice.CustomerId != customerId)
        {
            throw new ConflictException(
                "sales_invoice_receipt_customer_mismatch",
                "لا يمكن تخصيص سند قبض عميل على فاتورة تخص عميلاً آخر.");
        }
    }
}
