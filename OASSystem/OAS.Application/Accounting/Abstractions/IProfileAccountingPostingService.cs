namespace OAS.Application.Accounting.Abstractions;

public sealed record ProfileAccountingPostingLine(
    string ProfileDocumentType,
    string? AccountRole,
    bool UseSupplierSubledger,
    decimal DebitBase,
    decimal CreditBase,
    bool UseTransactionCurrency,
    string Description,
    Guid? SupplierId = null,
    Guid? ProductVariantId = null,
    Guid? WarehouseId = null,
    Guid? SourceLineId = null);

public sealed record ProfileAccountingPostingRequest(
    string Module,
    string SourceDocumentType,
    Guid SourceDocumentId,
    DateOnly PostingDate,
    DateOnly DocumentDate,
    string Description,
    Guid? TransactionCurrencyId,
    decimal ExchangeRate,
    IReadOnlyList<ProfileAccountingPostingLine> Lines);

public interface IProfileAccountingPostingService
{
    Task ValidatePostingPeriodAsync(DateOnly postingDate, CancellationToken cancellationToken = default);
    Task ValidateSupplierSubledgerAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<Guid> PostAsync(ProfileAccountingPostingRequest request, Guid postedBy, DateTime postedAtUtc, CancellationToken cancellationToken = default);
}
