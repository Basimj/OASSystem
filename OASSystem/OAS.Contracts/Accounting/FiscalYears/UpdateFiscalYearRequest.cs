namespace OAS.Contracts.Accounting.FiscalYears;

public sealed record UpdateFiscalYearRequest(
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string RowVersion);