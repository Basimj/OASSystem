using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Abstractions;

public interface ISupplierApReportingQueryService
{
    Task<SupplierStatementDto> GetSupplierStatementAsync(
        SupplierStatementFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<SupplierBalancesDto> GetSupplierBalancesAsync(
        SupplierBalancesFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<ApReconciliationDto> GetApReconciliationAsync(
        DateOnly asOfDate,
        Guid? controlAccountId = null,
        CancellationToken cancellationToken = default);
}
