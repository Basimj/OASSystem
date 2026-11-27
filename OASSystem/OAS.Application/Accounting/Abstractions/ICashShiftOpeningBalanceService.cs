namespace OAS.Application.Accounting.Abstractions;

public interface ICashShiftOpeningBalanceService
{
    Task<decimal> GetBookBalanceAsync(Guid cashAccountId, CancellationToken cancellationToken = default);
}
