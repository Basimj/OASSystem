namespace OAS.Application.Sales.Abstractions;

public interface ISalesPaymentAllocationTargetValidator
{
    Task ValidateAsync(
        Guid salesInvoiceId,
        Guid sourceCurrencyId,
        decimal allocatedAmount,
        decimal baseAllocatedAmount,
        Guid? excludingAllocationId = null,
        CancellationToken cancellationToken = default);
}
