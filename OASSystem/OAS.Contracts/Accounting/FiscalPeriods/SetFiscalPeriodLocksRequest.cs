namespace OAS.Contracts.Accounting.FiscalPeriods;

public sealed record SetFiscalPeriodLocksRequest(
    bool SalesLocked,
    bool InventoryLocked,
    bool AccountingLocked,
    string RowVersion);