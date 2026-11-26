using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Abstractions;

/// <summary>Cashbox reconciliation using linked movements, with timestamp fallback only for legacy shifts.</summary>
public interface ICashShiftBalanceService
{
    Task<decimal> CalculateExpectedAsync(CashShift shift, DateTime throughUtc, CancellationToken cancellationToken = default);
}
