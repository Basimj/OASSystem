using MediatR;
using OAS.Application.Accounting.Abstractions;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetGeneralLedger;

public sealed class GetGeneralLedgerQueryHandler(IAccountingReportingQueryService reports)
    : IRequestHandler<GetGeneralLedgerQuery, GeneralLedgerReportDto>
{
    public Task<GeneralLedgerReportDto> Handle(GetGeneralLedgerQuery request, CancellationToken cancellationToken) =>
        reports.GetGeneralLedgerAsync(
            new GeneralLedgerFilterDto(
                request.FromDate,
                request.ToDate,
                request.FiscalYearId,
                request.FiscalPeriodId,
                request.AccountId,
                request.IncludeAccountSubtree,
                request.JournalType,
                request.SourceModule,
                request.CustomerId,
                request.SupplierId,
                request.Reference,
                request.IncludeOpeningBalance),
            cancellationToken);
}
