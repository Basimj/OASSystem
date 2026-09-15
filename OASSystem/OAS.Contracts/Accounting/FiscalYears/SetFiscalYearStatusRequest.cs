using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.FiscalYears;

public sealed record SetFiscalYearStatusRequest(
    FiscalYearStatus Status,
    string RowVersion);