namespace OAS.Contracts.Accounting.FiscalPeriods;

public sealed record UpdateFiscalPeriodRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string RowVersion);