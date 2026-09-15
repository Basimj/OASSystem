using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.FiscalPeriods;

public sealed record SetFiscalPeriodStatusRequest(
    FiscalPeriodStatus Status,
    string RowVersion);