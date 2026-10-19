using OAS.Contracts.Sales.Reports;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesReportQueryService
{
    Task<SalesReportDto> GetAsync(SalesReportFilterDto filter, CancellationToken cancellationToken = default);
}
