using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.Abstractions;

public interface ISupplierCreditConcurrencyPort
{
    Task LockAsync(
        Guid purchaseReturnId,
        Guid purchaseInvoiceId,
        CancellationToken cancellationToken = default);
}
