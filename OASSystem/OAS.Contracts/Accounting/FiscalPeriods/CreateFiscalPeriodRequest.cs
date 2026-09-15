namespace OAS.Contracts.Accounting.FiscalPeriods;

public sealed record CreateFiscalPeriodRequest(
    Guid FiscalYearId,
    byte PeriodNumber,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);