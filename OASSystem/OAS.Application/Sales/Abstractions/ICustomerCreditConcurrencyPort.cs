namespace OAS.Application.Sales.Abstractions;

/// <summary>Uses ambient SQL transaction and serializes SUM/check/insert across a return and target.</summary>
public interface ICustomerCreditConcurrencyPort
{
    Task LockAsync(Guid salesReturnId, Guid? targetSalesInvoiceId = null, CancellationToken ct = default);
    Task LockReceiptAsync(Guid receiptVoucherLineId, CancellationToken ct = default);
}
