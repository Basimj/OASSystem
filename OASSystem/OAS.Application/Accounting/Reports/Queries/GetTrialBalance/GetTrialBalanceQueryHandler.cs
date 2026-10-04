using MediatR;
using OAS.Application.Accounting.Abstractions;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetTrialBalance;

public sealed class GetTrialBalanceQueryHandler(IAccountingReportingQueryService reports)
    : IRequestHandler<GetTrialBalanceQuery, TrialBalanceReportDto>
{
    public Task<TrialBalanceReportDto> Handle(GetTrialBalanceQuery request, CancellationToken cancellationToken) =>
        reports.GetTrialBalanceAsync(request.FromDate, request.ToDate, cancellationToken);
}
