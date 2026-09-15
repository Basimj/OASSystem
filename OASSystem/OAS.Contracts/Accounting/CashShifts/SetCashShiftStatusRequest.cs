using OAS.Contracts.Accounting.Enums;

namespace OAS.Contracts.Accounting.CashShifts;

public sealed record SetCashShiftStatusRequest(
    CashShiftStatus Status,
    string RowVersion);