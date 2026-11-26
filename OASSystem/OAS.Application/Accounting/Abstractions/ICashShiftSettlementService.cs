using OAS.Contracts.Accounting.CashShifts;

namespace OAS.Application.Accounting.Abstractions;

public interface ICashShiftSettlementService
{
    Task<IReadOnlyList<CashShiftSettlementAccountDto>> SettlementAccountsAsync(Guid shiftId, CancellationToken ct);
    Task<Guid> SettleAsync(Guid shiftId, CashShiftSettlementRequest request, CancellationToken ct);
}
