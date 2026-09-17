using OAS.Contracts.Accounting.CashShifts;
using OAS.Domain.Accounting.Entities;
using ContractCashShiftStatus = OAS.Contracts.Accounting.Enums.CashShiftStatus;

namespace OAS.Application.Accounting.CashShifts.Mapping;

public sealed class CashShiftMapper
{
    public CashShiftDto ToRead(CashShift source)
    {
        return new CashShiftDto(
            source.Id,
            source.ShiftNumber,
            source.CashAccountId,
            source.OpenedBy,
            source.OpenedAtUtc,
            source.OpeningBalance,
            source.ExpectedClosingBalance,
            source.ActualClosingBalance,
            source.DifferenceAmount,
            source.ClosedBy,
            source.ClosedAtUtc,
            (ContractCashShiftStatus)(int)source.Status,
            source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty);
    }
}
