namespace OAS.Contracts.Accounting.PaymentVouchers;

/// <summary>Posted book balance in the selected payment account's own currency; null means unavailable, not zero.</summary>
public sealed record PaymentVoucherBalanceDto(Guid DefinitionId, Guid? AccountId, Guid? CurrencyId,
    string? CurrencyCode, byte? DecimalPlaces, decimal? AvailableAmount, string? Error, string? PostingError = null);
