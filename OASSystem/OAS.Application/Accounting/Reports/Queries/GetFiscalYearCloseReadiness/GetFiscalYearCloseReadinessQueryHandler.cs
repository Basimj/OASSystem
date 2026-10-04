using MediatR;
using OAS.Application.Accounting.Abstractions;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetFiscalYearCloseReadiness;

public sealed class GetFiscalYearCloseReadinessQueryHandler(IAccountingReportingQueryService reports)
    : IRequestHandler<GetFiscalYearCloseReadinessQuery, FiscalCloseReadinessDto>
{
    public Task<FiscalCloseReadinessDto> Handle(GetFiscalYearCloseReadinessQuery request, CancellationToken cancellationToken) =>
        reports.GetFiscalYearCloseReadinessAsync(request.FiscalYearId, cancellationToken);
}
