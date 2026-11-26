using OAS.Contracts.Accounting.CashShifts;

namespace OAS.Application.Accounting.Abstractions;

public interface ICashShiftMovementQueryService
{
    Task<CashShiftMovementsDto> GetAsync(Guid shiftId, int page, CancellationToken ct);
}
