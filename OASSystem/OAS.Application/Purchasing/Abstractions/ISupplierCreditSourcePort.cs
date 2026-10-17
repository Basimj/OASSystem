namespace OAS.Application.Purchasing.Abstractions;

/// <summary>
/// Resolves the posted Supplier AP debit created by a purchase return.
/// Supplier credits must be derived from the posted journal on Supplier.AccountId,
/// not from the presence of a linked invoice alone.
/// </summary>
public interface ISupplierCreditSourcePort
{
    Task<decimal> GetPostedApCreditBaseAmountAsync(
        Guid purchaseReturnId,
        Guid journalEntryId,
        Guid supplierId,
        CancellationToken cancellationToken = default);
}
