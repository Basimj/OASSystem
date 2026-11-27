using OAS.Contracts.Sales.Reports;

namespace OAS.Application.Sales.Abstractions;

public interface ISalesReportQueryService
{
    Task<SalesReportOptionPage> GetOptionsAsync(SalesReportOptionRequest request, CancellationToken cancellationToken = default);
    Task<SalesReportDto> GetAsync(SalesReportFilterDto filter, CancellationToken cancellationToken = default);
}
