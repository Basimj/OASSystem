namespace OAS.Application.Accounting.Abstractions;

public sealed record SupplierOpeningJournalExpectation(
    Guid SupplierId,
    Guid SupplierAccountId,
    decimal BaseAmount);

public interface ISupplierOpeningJournalVerificationPort
{
    Task VerifyExistingJournalAsync(
        Guid journalEntryId,
        DateOnly asOfDate,
        IReadOnlyCollection<SupplierOpeningJournalExpectation> expectations,
        CancellationToken cancellationToken = default);
}

public interface ISupplierOpeningConcurrencyPort
{
    Task LockPaymentLineAndOpeningItemAsync(
        Guid paymentVoucherLineId,
        Guid supplierOpeningItemId,
        CancellationToken cancellationToken = default);
}
