using MediatR;
using FluentValidation;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetBalanceSheet;

public sealed record GetBalanceSheetQuery(BalanceSheetFilterDto Filter) : IQuery<BalanceSheetReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [JournalPermissions.View];
}
public sealed class GetBalanceSheetQueryValidator : AbstractValidator<GetBalanceSheetQuery>
{
    public GetBalanceSheetQueryValidator() => RuleFor(x => x.Filter.AsOfDate).NotEmpty().WithErrorCode("report_date_required");
}
public sealed class GetBalanceSheetQueryHandler(IAccountingReportingQueryService reports) : IRequestHandler<GetBalanceSheetQuery, BalanceSheetReportDto>
{
    public Task<BalanceSheetReportDto> Handle(GetBalanceSheetQuery request, CancellationToken cancellationToken) => reports.GetBalanceSheetAsync(request.Filter, cancellationToken);
}
