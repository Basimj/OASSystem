using OAS.Contracts.Accounting.Reports;
namespace OAS.Application.Accounting.Abstractions;
public interface ICashBankReportQueryService
{
    Task<CashBankReportDto> GetAsync(CashBankReportFilterDto filter, CancellationToken ct);
}
