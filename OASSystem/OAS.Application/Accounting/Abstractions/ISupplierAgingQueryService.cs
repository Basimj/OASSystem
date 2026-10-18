using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Abstractions;

public interface ISupplierAgingQueryService
{
    Task<SupplierAgingReportDto> GetAsync(
        SupplierAgingFilterDto filter,
        CancellationToken cancellationToken = default);
}
