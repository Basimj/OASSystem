using MediatR;
using OAS.Application.Accounting.Abstractions;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetFiscalPeriodCloseReadiness;

public sealed class GetFiscalPeriodCloseReadinessQueryHandler(IAccountingReportingQueryService reports)
    : IRequestHandler<GetFiscalPeriodCloseReadinessQuery, FiscalCloseReadinessDto>
{
    public Task<FiscalCloseReadinessDto> Handle(GetFiscalPeriodCloseReadinessQuery request, CancellationToken cancellationToken) =>
        reports.GetFiscalPeriodCloseReadinessAsync(request.FiscalPeriodId, cancellationToken);
}
