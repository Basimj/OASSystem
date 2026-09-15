namespace OAS.Contracts.Accounting.FiscalYears;

public sealed record CreateFiscalYearRequest(
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);