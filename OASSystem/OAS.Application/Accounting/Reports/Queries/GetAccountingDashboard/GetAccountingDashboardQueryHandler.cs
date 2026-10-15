using MediatR;
using OAS.Application.Accounting.Abstractions;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetAccountingDashboard;

public sealed class GetAccountingDashboardQueryHandler(IAccountingReportingQueryService reports)
    : IRequestHandler<GetAccountingDashboardQuery, AccountingDashboardDto>
{
    public Task<AccountingDashboardDto> Handle(
        GetAccountingDashboardQuery request,
        CancellationToken cancellationToken) =>
        reports.GetAccountingDashboardAsync(cancellationToken);
}
