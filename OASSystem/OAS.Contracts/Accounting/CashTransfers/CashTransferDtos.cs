namespace OAS.Contracts.Accounting.CashTransfers;

public sealed record CashTransferAccountDto(Guid AccountId, string Name, string Kind, Guid CurrencyId, string CurrencyCode, byte DecimalPlaces)
{
    // Populated only for dated lookups. Never infer zero from missing/ambiguous history.
    public decimal? BookBalance { get; init; }
    public decimal? BookBaseBalance { get; init; }
    public string? BalanceError { get; init; }
    public decimal? AccountingRate { get; init; }
    public DateOnly? RateDate { get; init; }
}
public sealed record CashTransferFeeAccountDto(Guid AccountId, string Name);
public sealed record CashTransferLookupsDto(IReadOnlyList<CashTransferAccountDto> Accounts, IReadOnlyList<CashTransferFeeAccountDto> FeeAccounts, string BaseCurrencyCode)
{
    public byte BaseDecimalPlaces { get; init; }
}
public sealed record CashTransferRequest(DateOnly Date, Guid SourceAccountId, Guid DestinationAccountId, decimal SourceAmount,
    decimal DestinationAmount, decimal FeeAmount, Guid? FeeAccountId, string? Reference, string Description, string? RowVersion = null);
public sealed record CashTransferActionRequest(string RowVersion, DateOnly? ReversalDate = null, string? Reason = null);
public sealed record CashTransferDto(Guid Id, string Number, DateOnly Date, Guid SourceAccountId, Guid DestinationAccountId,
    string SourceName, string DestinationName, string SourceCurrencyCode, string DestinationCurrencyCode,
    decimal SourceAmount, decimal DestinationAmount, decimal FeeAmount, Guid? FeeAccountId, string? Reference, string Description,
    int Status, Guid? JournalEntryId, Guid? ReversalJournalEntryId, DateOnly? ReversalDate, string? ReversalReason,
    decimal? SourceBaseAmount, decimal? DestinationBaseAmount, decimal? FeeBaseAmount, decimal? FxDifferenceBase, string RowVersion,
    string? SourceKind = null, string? DestinationKind = null, bool IsCrossCurrency = false);
public sealed record CashTransferPageDto(IReadOnlyList<CashTransferDto> Items, int TotalCount, int Page);

/// <summary>Arithmetic for the proposed transfer quote, not the posting valuation.</summary>
public static class CashTransferPreviewMath
{
    public static decimal? SourceUnitsPerDestination(decimal? sourceToBase, decimal? destinationToBase)
    {
        if (sourceToBase is not decimal source || source <= 0 || destinationToBase is not decimal destination || destination <= 0)
            return null;
        try { var rate = destination / source; return rate > 0 ? rate : null; }
        catch (OverflowException) { return null; }
    }

    public static decimal Received(decimal grossSource, decimal fee, decimal? sourcePerDestination, byte destinationDecimals)
    {
        if (grossSource <= 0 || grossSource >= 1000000000000000m || fee < 0 || fee >= grossSource ||
            sourcePerDestination is not decimal rate || rate <= 0 || destinationDecimals > 4)
            return 0m;
        try
        {
            var received = Math.Round((grossSource - fee) / rate, destinationDecimals, MidpointRounding.AwayFromZero);
            return received < 1000000000000000m ? received : 0m;
        }
        catch (OverflowException) { return 0m; }
    }
}
