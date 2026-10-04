using OAS.Contracts.Sales.Commissions;
namespace OAS.Application.Sales.Abstractions;
public interface ICommissionQueryService
{
    Task<IReadOnlyList<CommissionRuleDto>> GetRulesAsync(Guid? employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommissionStatementDto>> GetStatementsAsync(Guid? employeeId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);
    Task<CommissionStatementDto?> GetStatementAsync(Guid id, CancellationToken cancellationToken = default);
}
